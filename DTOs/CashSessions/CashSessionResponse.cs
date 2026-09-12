namespace SecureSistem.DTOs.CashSessions
{
    public class CashSessionResponse
    {
        public int Id { get; set; }
        public int CashRegisterId { get; set; }
        public string CashRegisterName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal OpeningAmount { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal? ClosingAmount { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? Difference { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool IsOpen { get; set; }
        public string? Notes { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
