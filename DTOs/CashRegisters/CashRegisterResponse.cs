namespace SecureSistem.DTOs.CashRegisters
{
    public class CashRegisterResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public bool IsActive { get; set; }
        public bool HasOpenSession { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
