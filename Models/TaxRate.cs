namespace SecureSistem.Models
{
    /// <summary>
    /// A reusable tax rate (e.g. "IVA 16%") that can be assigned to products.
    /// </summary>
    public class TaxRate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tax rate as a fraction (e.g. 0.16 for 16%).
        /// </summary>
        public decimal Rate { get; set; }

        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
    }
}
