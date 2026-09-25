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

        /// <summary>
        /// When true, this product is a combo/kit sold as its own catalog entry (own price,
        /// tax, image) but carrying no stock of its own — selling it deducts stock from its
        /// ProductComboItems components instead. See ProductComboItem.
        /// </summary>
        public bool IsCombo { get; set; } = false;

        /// <summary>
        /// The product's primary/consignor supplier. When set and Supplier.IsConsignor is
        /// true, every sale of this product auto-generates a ConsignmentSale using
        /// CommissionType/CommissionValue below. Optional metadata (who to reorder from)
        /// when the supplier isn't a consignor.
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// "Percentage" (CommissionValue is the store's cut, e.g. 30 = store keeps 30%, pays
        /// the consignor 70%) or "FixedAmount" (CommissionValue is the peso amount paid to
        /// the consignor per unit sold, regardless of price). Only meaningful when SupplierId
        /// is a consignor.
        /// </summary>
        public string? CommissionType { get; set; }

        public decimal? CommissionValue { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public Category? Category { get; set; }
        public TaxRate? TaxRate { get; set; }
        public Supplier? Supplier { get; set; }
    }
}
