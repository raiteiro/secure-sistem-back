namespace SecureSistem.Models
{
    /// <summary>
    /// Single-use token that authorizes a password reset without knowing the current password.
    /// </summary>
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }

        public bool IsActive => UsedAt is null && ExpiresAt > DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
    }
}
