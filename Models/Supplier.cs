namespace SecureSistem.Models
{
    /// <summary>
    /// A supplier a company buys from, or receives goods on consignment from. See
    /// IsConsignor — when true, products attributed to this supplier (Product.SupplierId)
    /// generate a ConsignmentSale every time they sell, instead of being owned outright.
    /// </summary>
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        /// <summary>
        /// True if this supplier leaves inventory on consignment (paid only for what sells)
        /// instead of selling it outright to the company at time of receipt.
        /// </summary>
        public bool IsConsignor { get; set; } = false;

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
