using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Companies
{
    public class CreateCompanyRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TaxId { get; set; }

        /// <summary>
        /// Fiscal/mailing address, used as the company header on generated PDF reports.
        /// </summary>
        [MaxLength(300)]
        public string? Address { get; set; }

        /// <summary>
        /// Contact phone number, used as the company header on generated PDF reports.
        /// </summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// Contact email, used as the company header on generated PDF reports.
        /// </summary>
        [EmailAddress]
        [MaxLength(256)]
        public string? Email { get; set; }

        /// <summary>
        /// Website URL, used as the company header on generated PDF reports.
        /// </summary>
        [Url]
        [MaxLength(300)]
        public string? Website { get; set; }

        /// <summary>
        /// Instagram profile URL.
        /// </summary>
        [Url]
        [MaxLength(300)]
        public string? Instagram { get; set; }

        /// <summary>
        /// Facebook profile/page URL.
        /// </summary>
        [Url]
        [MaxLength(300)]
        public string? Facebook { get; set; }

        /// <summary>
        /// TikTok profile URL.
        /// </summary>
        [Url]
        [MaxLength(300)]
        public string? TikTok { get; set; }

        /// <summary>
        /// WhatsApp contact number.
        /// </summary>
        [MaxLength(20)]
        public string? WhatsApp { get; set; }

        /// <summary>
        /// Id of the curated color palette for this company's theme (e.g. "ocean", "emerald").
        /// Null uses the default ("purple") look.
        /// </summary>
        [RegularExpression("^(purple|ocean|emerald|teal|ruby|amber|rose|indigo|slate|sky-light)$",
            ErrorMessage = "ColorPreset debe ser uno de los ids de paleta conocidos.")]
        public string? ColorPreset { get; set; }

        public int? MaxUsers { get; set; }
        public int? MaxConcurrentSessions { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
    }
}
