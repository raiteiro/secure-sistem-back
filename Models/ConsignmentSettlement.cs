namespace SecureSistem.Models
{
    /// <summary>
    /// A batch payment to a consignor for a set of ConsignmentSale rows, which get stamped
    /// with this settlement's Id and stop counting toward that supplier's pending balance.
    /// </summary>
    public class ConsignmentSettlement
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public int CompanyId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Supplier Supplier { get; set; } = null!;
        public Company Company { get; set; } = null!;
        public ICollection<ConsignmentSale> Sales { get; set; } = new List<ConsignmentSale>();
    }
}
