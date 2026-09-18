using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SecureSistem.Models;

namespace SecureSistem.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user, string roleName, string companyName, int sessionId)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, roleName),
                new("roleId", user.RoleId.ToString()),
                new("companyId", user.CompanyId.ToString()),
                new("companyName", companyName),
                new("isSystemAdmin", user.IsSystemAdmin.ToString()),
                new("firstName", user.FirstName),
                new("lastName", user.LastName),
                new("sessionId", sessionId.ToString())
            };

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: GetExpiration(),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime GetExpiration()
        {
            var hours = int.Parse(_configuration["Jwt:ExpirationHours"] ?? "2");
            return DateTime.UtcNow.AddHours(hours);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        public DateTime GetRefreshTokenExpiration()
        {
            var days = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
            return DateTime.UtcNow.AddDays(days);
        }
    }
}
