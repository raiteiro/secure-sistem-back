using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.Users;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for user management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active users for the authenticated user's company.
        /// System administrators see users across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<UserResponse>), 200)]
        public async Task<ActionResult<PagedResponse<UserResponse>>> GetAll(
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var query = _context.Users.Include(u => u.Role).Where(u => u.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(u => u.CompanyId == GetCompanyId());

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .Select(u => MapToResponse(u))
                .ToListAsync();

            return Ok(users.ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets a user by ID. System administrators can access users from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserResponse>> GetById(int id)
        {
            var query = _context.Users.Include(u => u.Role).Where(u => u.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(u => u.CompanyId == GetCompanyId());

            var user = await query.FirstOrDefaultAsync();

            if (user is null)
                return NotFound(new { message = "Usuario no encontrado." });

            return Ok(MapToResponse(user));
        }

        /// <summary>
        /// Creates a new user. System administrators may pass a CompanyId to create
        /// the user directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UserResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Solo el administrador del sistema puede crear usuarios en otra empresa." });

                companyId = request.CompanyId.Value;
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId && c.IsActive);
            if (company is null)
                return BadRequest(new { message = "Empresa inválida." });

            if (company.MaxUsers is not null)
            {
                var activeUserCount = await _context.Users
                    .CountAsync(u => u.CompanyId == companyId && u.IsActive);
                if (activeUserCount >= company.MaxUsers)
                    return BadRequest(new
                    {
                        message = $"Se alcanzó el máximo de usuarios permitidos para tu plan ({company.MaxUsers})."
                    });
            }

            // Validate unique username within company
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username && u.CompanyId == companyId);
            if (usernameExists)
                return BadRequest(new { message = "El nombre de usuario ya existe en esta empresa." });

            // Validate unique email globally
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                return BadRequest(new { message = "El correo ya está en uso." });

            // Validate role belongs to same company
            var roleExists = await _context.Roles
                .AnyAsync(r => r.Id == request.RoleId && r.CompanyId == companyId && r.IsActive);
            if (!roleExists)
                return BadRequest(new { message = "Rol inválido." });

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                RoleId = request.RoleId,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTimeHelper.Now,
                CreatedBy = currentUser
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload with Role for response
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            _logger.LogInformation("User created: {Id} - {Username} by {CreatedBy}", user.Id, user.Username, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapToResponse(user));
        }

        /// <summary>
        /// Updates an existing user. System administrators can update users from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(UserResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UpdateUserRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Users.Include(u => u.Role).Where(u => u.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(u => u.CompanyId == GetCompanyId());

            var user = await query.FirstOrDefaultAsync();

            if (user is null)
                return NotFound(new { message = "Usuario no encontrado." });

            // Validate unique username (exclude current user)
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username && u.CompanyId == user.CompanyId && u.Id != id);
            if (usernameExists)
                return BadRequest(new { message = "El nombre de usuario ya existe en esta empresa." });

            // Validate unique email (exclude current user)
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != id);
            if (emailExists)
                return BadRequest(new { message = "El correo ya está en uso." });

            // Validate role
            var roleExists = await _context.Roles
                .AnyAsync(r => r.Id == request.RoleId && r.CompanyId == user.CompanyId && r.IsActive);
            if (!roleExists)
                return BadRequest(new { message = "Rol inválido." });

            user.Username = request.Username;
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.RoleId = request.RoleId;
            user.ModifiedAt = DateTimeHelper.Now;
            user.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            _logger.LogInformation("User updated: {Id} - {Username} by {ModifiedBy}", user.Id, user.Username, currentUser);

            return Ok(MapToResponse(user));
        }

        /// <summary>
        /// Deactivates a user (soft delete). System administrators can deactivate users
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Users.Where(u => u.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(u => u.CompanyId == GetCompanyId());

            var user = await query.FirstOrDefaultAsync();

            if (user is null)
                return NotFound(new { message = "Usuario no encontrado." });

            user.IsActive = false;
            user.ModifiedAt = DateTimeHelper.Now;
            user.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User deactivated: {Id} - {Username} by {ModifiedBy}", user.Id, user.Username, currentUser);

            return Ok(new { message = "Usuario desactivado correctamente." });
        }

        /// <summary>
        /// Resets a user's password to a temporary one, returned in the response so an
        /// administrator can hand it to the user (e.g. when they forgot it and can't use
        /// the self-service email flow). Forces a password change on next login and
        /// revokes the user's active sessions. System administrators can reset users from
        /// any company; everyone else only within their own.
        /// </summary>
        [HttpPost("{id:int}/reset-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var query = _context.Users.Where(u => u.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(u => u.CompanyId == GetCompanyId());

            var user = await query.FirstOrDefaultAsync();
            if (user is null)
                return NotFound(new { message = "Usuario no encontrado." });

            var currentUser = GetCurrentUsername();
            var temporaryPassword = GenerateTemporaryPassword();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            user.MustChangePassword = true;
            user.ModifiedAt = DateTimeHelper.Now;
            user.ModifiedBy = currentUser;

            var activeSessions = await _context.RefreshTokens
                .Where(rt => rt.UserId == id && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var session in activeSessions)
                session.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset for user {Id} - {Username} by {ModifiedBy}",
                user.Id, user.Username, currentUser);

            return Ok(new { message = "Contraseña restablecida correctamente.", temporaryPassword });
        }

        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var bytes = RandomNumberGenerator.GetBytes(12);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }

        /// <summary>
        /// Moves an existing user to a different company, assigning a role from the target
        /// company. Revokes the user's active sessions and clears any route assignments
        /// made directly to them, since those belonged to their previous company. System
        /// administrator only.
        /// </summary>
        [HttpPost("{id:int}/reassign-company")]
        [ProducesResponseType(typeof(UserResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserResponse>> ReassignCompany(int id, [FromBody] ReassignUserCompanyRequest request)
        {
            if (!IsSystemAdmin())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede reasignar la empresa de un usuario." });

            var currentUser = GetCurrentUsername();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
                return NotFound(new { message = "Usuario no encontrado." });

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == request.CompanyId && c.IsActive);
            if (company is null)
                return BadRequest(new { message = "Empresa inválida." });

            if (company.MaxUsers is not null && user.CompanyId != request.CompanyId)
            {
                var activeUserCount = await _context.Users
                    .CountAsync(u => u.CompanyId == request.CompanyId && u.IsActive);
                if (activeUserCount >= company.MaxUsers)
                    return BadRequest(new
                    {
                        message = $"La empresa destino ya alcanzó su máximo de usuarios permitidos ({company.MaxUsers})."
                    });
            }

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == user.Username && u.CompanyId == request.CompanyId && u.Id != id);
            if (usernameExists)
                return BadRequest(new { message = "El nombre de usuario ya existe en la empresa destino." });

            var roleExists = await _context.Roles
                .AnyAsync(r => r.Id == request.RoleId && r.CompanyId == request.CompanyId && r.IsActive);
            if (!roleExists)
                return BadRequest(new { message = "Rol inválido para la empresa destino." });

            user.CompanyId = request.CompanyId;
            user.RoleId = request.RoleId;
            user.ModifiedAt = DateTimeHelper.Now;
            user.ModifiedBy = currentUser;

            // Direct route assignments belonged to the previous company's route tree.
            var directRoutes = await _context.UserNavigationRoutes
                .Where(ur => ur.UserId == id && ur.IsActive)
                .ToListAsync();
            foreach (var route in directRoutes)
                route.IsActive = false;

            // Force re-login so the JWT is reissued with the new company/role claims.
            var activeSessions = await _context.RefreshTokens
                .Where(rt => rt.UserId == id && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var session in activeSessions)
                session.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            _logger.LogInformation("User {Id} reassigned to company {CompanyId} by {ModifiedBy}",
                user.Id, user.CompanyId, currentUser);

            return Ok(MapToResponse(user));
        }

        private static UserResponse MapToResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name ?? string.Empty,
                CompanyId = user.CompanyId,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy,
                ModifiedAt = user.ModifiedAt,
                ModifiedBy = user.ModifiedBy
            };
        }
    }
}
