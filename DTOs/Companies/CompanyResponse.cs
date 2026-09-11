namespace SecureSistem.DTOs.Companies
{
    public class CompanyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? TikTok { get; set; }
        public string? WhatsApp { get; set; }
        public string? LogoPath { get; set; }
        public string? ColorPreset { get; set; }
        public bool IsActive { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxConcurrentSessions { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public int ActiveUserCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
