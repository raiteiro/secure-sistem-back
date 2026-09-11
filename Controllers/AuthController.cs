using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.Auth;
using SecureSistem.Models;
using SecureSistem.Services;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Handles user authentication and session management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(402)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            // Search by username or email since email is globally unique
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u =>
                    (u.Username == request.Username || u.Email == request.Username)
                    && u.IsActive);

            if (user is null)
            {
                _logger.LogWarning("Login failed: user '{Username}' not found",
                    request.Username);
                return Unauthorized(new { message = "Invalid credentials." });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: invalid password for user '{Username}'", request.Username);
                return Unauthorized(new { message = "Invalid credentials." });
            }

            if (!user.Company.IsActive)
            {
                _logger.LogWarning("Login blocked: company '{Company}' is inactive", user.Company.Name);
                return StatusCode(403, new { message = "Tu empresa ha sido desactivada. Contacta a soporte." });
            }

            if (user.Company.SubscriptionExpiresAt is not null
                && user.Company.SubscriptionExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Login blocked: company '{Company}' subscription expired",
                    user.Company.Name);
                return StatusCode(402, new
                {
                    message = "La suscripción de tu empresa ha vencido. Contacta a soporte para renovar tu plan."
                });
            }

            if (user.Company.MaxConcurrentSessions is not null)
            {
                var activeSessions = await _context.RefreshTokens
                    .Where(rt => rt.User.CompanyId == user.CompanyId
                        && rt.RevokedAt == null
                        && rt.ExpiresAt > DateTime.UtcNow)
                    .CountAsync();

                if (activeSessions >= user.Company.MaxConcurrentSessions)
                {
                    _logger.LogWarning("Login blocked: company '{Company}' reached max concurrent sessions ({Max})",
                        user.Company.Name, user.Company.MaxConcurrentSessions);
                    return StatusCode(403, new
                    {
                        message = "Se alcanzó el máximo de sesiones simultáneas permitidas para tu plan. Cierra sesión en otro dispositivo e intenta de nuevo."
                    });
                }
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;

            var refreshToken = new RefreshToken
            {
                Token = _tokenService.GenerateRefreshToken(),
                UserId = user.Id,
                ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user, user.Role.Name, user.Company.Name);
            var expiration = _tokenService.GetExpiration();

            _logger.LogInformation("User '{Username}' logged in successfully", user.Username);

            return Ok(new LoginResponse
            {
                Token = token,
                ExpiresAt = expiration,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                User = new UserInfoResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleName = user.Role.Name,
                    RoleId = user.RoleId,
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company.Name,
                    IsSystemAdmin = user.IsSystemAdmin,
                    MustChangePassword = user.MustChangePassword
                }
            });
        }

        /// <summary>
        /// Exchanges a valid refresh token for a new access token, rotating the refresh token.
        /// Intended to be called periodically by the frontend while the session is active,
        /// so the user is kept logged in without re-entering credentials.
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(402)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User).ThenInclude(u => u.Role)
                .Include(rt => rt.User).ThenInclude(u => u.Company)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (storedToken is null || !storedToken.IsActive || !storedToken.User.IsActive)
            {
                _logger.LogWarning("Refresh token failed: token not found, expired, revoked or user inactive");
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            var user = storedToken.User;

            if (!user.Company.IsActive)
            {
                _logger.LogWarning("Refresh blocked: company '{Company}' is inactive", user.Company.Name);
                return StatusCode(403, new { message = "Tu empresa ha sido desactivada. Contacta a soporte." });
            }

            if (user.Company.SubscriptionExpiresAt is not null
                && user.Company.SubscriptionExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh blocked: company '{Company}' subscription expired", user.Company.Name);
                return StatusCode(402, new
                {
                    message = "La suscripción de tu empresa ha vencido. Contacta a soporte para renovar tu plan."
                });
            }

            var newRefreshToken = new RefreshToken
            {
                Token = _tokenService.GenerateRefreshToken(),
                UserId = user.Id,
                ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
                CreatedAt = DateTime.UtcNow
            };

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshToken.Token;
            _context.RefreshTokens.Add(newRefreshToken);

            var accessToken = _tokenService.GenerateToken(user, user.Role.Name, user.Company.Name);
            var expiration = _tokenService.GetExpiration();

            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                Token = accessToken,
                ExpiresAt = expiration,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
                User = new UserInfoResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleName = user.Role.Name,
                    RoleId = user.RoleId,
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company.Name,
                    IsSystemAdmin = user.IsSystemAdmin,
                    MustChangePassword = user.MustChangePassword
                }
            });
        }

        /// <summary>
        /// Revokes a refresh token, ending that session. Does not require a valid access
        /// token — logout must work even after the access token has already expired,
        /// since the refresh token itself is what's being revoked.
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (storedToken is not null && storedToken.RevokedAt is null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully." });
        }

        /// <summary>
        /// Requests a password reset link by email. Always responds with a generic success
        /// message, whether or not the email is registered, so callers can't enumerate accounts.
        /// </summary>
        [HttpPost("forgot-password")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user is not null)
            {
                var expirationMinutes = int.Parse(_configuration["PasswordReset:TokenExpirationMinutes"] ?? "30");

                var resetToken = new PasswordResetToken
                {
                    Token = GenerateSecureToken(),
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    CreatedAt = DateTime.UtcNow
                };
                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
                var resetLink = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken.Token)}";

                try
                {
                    await _emailService.SendAsync(
                        user.Email,
                        "Restablecer contraseña",
                        $"Solicitaste restablecer tu contraseña. Este enlace vence en {expirationMinutes} minutos:\n\n{resetLink}\n\nSi no fuiste tú, ignora este correo.");

                    _logger.LogInformation("Password reset email sent to user '{Username}'", user.Username);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email for user '{Username}'", user.Username);
                }
            }

            return Ok(new { message = "Si el correo está registrado, se envió un enlace para restablecer la contraseña." });
        }

        /// <summary>
        /// Sets a new password using a token obtained from the forgot-password email.
        /// Revokes all of the user's active sessions.
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token);

            if (resetToken is null || !resetToken.IsActive || !resetToken.User.IsActive)
                return BadRequest(new { message = "Invalid or expired reset token." });

            var user = resetToken.User;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            resetToken.UsedAt = DateTime.UtcNow;

            var activeSessions = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var session in activeSessions)
                session.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset completed for user '{Username}'", user.Username);

            return Ok(new { message = "Password reset successfully." });
        }

        private static string GenerateSecureToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        /// <summary>
        /// Returns the current authenticated user's info.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserInfoResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserInfoResponse>> Me()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user is null)
                return Unauthorized();

            return Ok(new UserInfoResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = user.Role.Name,
                RoleId = user.RoleId,
                CompanyId = user.CompanyId,
                CompanyName = user.Company.Name,
                IsSystemAdmin = user.IsSystemAdmin,
                MustChangePassword = user.MustChangePassword
            });
        }

        /// <summary>
        /// Changes the password of the currently authenticated user.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);

            if (user is null)
                return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User '{Username}' changed their password", user.Username);

            return Ok(new { message = "Password changed successfully." });
        }
    }
}
