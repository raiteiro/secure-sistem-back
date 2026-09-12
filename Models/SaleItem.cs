namespace SecureSistem.Models
{
    /// <summary>
    /// A line item of a sale. UnitPrice and the tax rate applied are snapshotted from the
    /// product at the time of sale, so later catalog changes never alter historical sales.
    /// </summary>
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Tax rate applied (fraction, e.g. 0.16), snapshotted from the product's tax rate.
        /// </summary>
        public decimal TaxRateValue { get; set; }

        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Quantity * UnitPrice - DiscountAmount (before tax).
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Subtotal + TaxAmount.
        /// </summary>
        public decimal Total { get; set; }

        // Navigation properties
        public Sale Sale { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
