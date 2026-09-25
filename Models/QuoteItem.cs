namespace SecureSistem.Models
{
    /// <summary>
    /// One line of a Quote — price/tax snapshotted the same way SaleItem is, so a quote
    /// keeps showing what was actually offered even if the product's catalog price changes
    /// before it's converted (or never is).
    /// </summary>
    public class QuoteItem
    {
        public int Id { get; set; }
        public int QuoteId { get; set; }
        public int ProductId { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRateValue { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }

        // Navigation properties
        public Quote Quote { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
