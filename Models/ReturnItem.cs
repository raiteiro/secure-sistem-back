namespace SecureSistem.Models
{
    /// <summary>
    /// A returned quantity of one line of the original sale. UnitPrice and the tax rate are
    /// snapshotted from the SaleItem (proportional to its per-unit discount), so refunds
    /// stay consistent even if the product's price/tax rate changes afterwards.
    /// </summary>
    public class ReturnItem
    {
        public int Id { get; set; }
        public int ReturnId { get; set; }
        public int SaleItemId { get; set; }
        public int ProductId { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRateValue { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }

        // Navigation properties
        public Return Return { get; set; } = null!;
        public SaleItem SaleItem { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
