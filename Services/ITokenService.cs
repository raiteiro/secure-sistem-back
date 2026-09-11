using SecureSistem.Models;

namespace SecureSistem.Services
{
    /// <summary>
    /// Generates and validates JWT tokens for authentication.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for the given user.
        /// </summary>
        string GenerateToken(User user, string roleName, string companyName);

        /// <summary>
        /// Gets the access token expiration time.
        /// </summary>
        DateTime GetExpiration();

        /// <summary>
        /// Generates a cryptographically secure refresh token.
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Gets the refresh token expiration time.
        /// </summary>
        DateTime GetRefreshTokenExpiration();
    }
}
