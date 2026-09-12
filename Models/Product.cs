namespace SecureSistem.Models
{
    /// <summary>
    /// A sellable product in the catalog. Shared across all of a company's branches;
    /// stock levels per warehouse are tracked separately in Inventory.
    /// </summary>
    public class Product
    {
        public int Id { get; set; }

        /// <summary>
        /// Barcode/SKU. Optional (not every item needs one), unique per company when set.
        /// </summary>
        public string? Sku { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// Unit of measure (e.g. "Pieza", "Kg", "Litro"). Free text for flexibility.
        /// </summary>
        public string? Unit { get; set; }

        public decimal Price { get; set; }

        /// <summary>
        /// Cost per unit. Null means unknown/not tracked yet.
        /// </summary>
        public decimal? Cost { get; set; }

        /// <summary>
        /// Relative path (under wwwroot) to the product's image. Null means no image uploaded.
        /// </summary>
        public string? ImagePath { get; set; }

        public int? CategoryId { get; set; }
        public int? TaxRateId { get; set; }

        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public Category? Category { get; set; }
        public TaxRate? TaxRate { get; set; }
    }
}
