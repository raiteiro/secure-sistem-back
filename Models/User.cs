namespace SecureSistem.Models
{
    /// <summary>
    /// Represents an application user.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// System administrators can view and manage data across all companies, not just their own.
        /// </summary>
        public bool IsSystemAdmin { get; set; } = false;

        /// <summary>
        /// When true, the frontend must force the user to set a new password before
        /// letting them use the rest of the system (e.g. right after account creation).
        /// </summary>
        public bool MustChangePassword { get; set; } = false;

        public int RoleId { get; set; }
        public int CompanyId { get; set; }

        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// UTC timestamp of the last authenticated API request this user made (stamped by
        /// middleware, not shown to the user). Used to detect real inactivity — unlike a
        /// refresh token's sliding expiration, this doesn't move just because the frontend
        /// silently calls /auth/refresh in the background.
        /// </summary>
        public DateTime? LastSeenAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Role Role { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
