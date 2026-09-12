namespace SecureSistem.Models
{
    /// <summary>
    /// A physical/logical cash register at a branch. Cashiers open/close shifts
    /// (CashSession) against one of these.
    /// </summary>
    public class CashRegister
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int BranchId { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Branch Branch { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
