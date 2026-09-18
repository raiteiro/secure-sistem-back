using SecureSistem.Models;

namespace SecureSistem.Services
{
    /// <summary>
    /// Generates and validates JWT tokens for authentication.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for the given user, tagged with the id of the RefreshToken
        /// session it was issued alongside — lets the middleware invalidate this access token
        /// immediately if that session gets revoked, instead of waiting for it to expire.
        /// </summary>
        string GenerateToken(User user, string roleName, string companyName, int sessionId);

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
