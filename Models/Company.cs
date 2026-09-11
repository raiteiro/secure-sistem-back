namespace SecureSistem.Models
{
    /// <summary>
    /// Represents a company/tenant in the multi-tenant system.
    /// </summary>
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tax identification number (RFC, NIT, etc.).
        /// </summary>
        public string? TaxId { get; set; }

        /// <summary>
        /// Fiscal/mailing address, used as the company header on generated PDF reports.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Contact phone number, used as the company header on generated PDF reports.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Contact email, used as the company header on generated PDF reports.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Website URL, used as the company header on generated PDF reports.
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Instagram profile URL.
        /// </summary>
        public string? Instagram { get; set; }

        /// <summary>
        /// Facebook profile/page URL.
        /// </summary>
        public string? Facebook { get; set; }

        /// <summary>
        /// TikTok profile URL.
        /// </summary>
        public string? TikTok { get; set; }

        /// <summary>
        /// WhatsApp contact number.
        /// </summary>
        public string? WhatsApp { get; set; }

        /// <summary>
        /// Relative path (under wwwroot) to the company's logo image, used on generated
        /// PDF reports. Null means no logo has been uploaded.
        /// </summary>
        public string? LogoPath { get; set; }

        /// <summary>
        /// Id of the curated color palette used to theme the authenticated area for this
        /// company's users (e.g. "ocean", "emerald"). Null means the default ("purple") look.
        /// </summary>
        public string? ColorPreset { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Maximum number of active users allowed under this company's plan. Null means unlimited.
        /// </summary>
        public int? MaxUsers { get; set; }

        /// <summary>
        /// Maximum number of concurrent logged-in sessions allowed under this company's plan. Null means unlimited.
        /// </summary>
        public int? MaxConcurrentSessions { get; set; }

        /// <summary>
        /// Date until which the company's subscription is paid. Null means no subscription restriction.
        /// </summary>
        public DateTime? SubscriptionExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<NavigationRoute> NavigationRoutes { get; set; } = new List<NavigationRoute>();
    }
}
