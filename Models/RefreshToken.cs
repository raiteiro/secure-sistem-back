namespace SecureSistem.Models
{
    /// <summary>
    /// Represents a refresh token issued to a user session.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }

        public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
