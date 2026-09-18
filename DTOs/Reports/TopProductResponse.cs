namespace SecureSistem.DTOs.Reports
{
    public class TopProductResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public decimal QuantitySold { get; set; }

        /// <summary>
        /// Total charged for this product (subtotal after discount, plus tax) — same as
        /// summing SaleItem.Total across the matching lines.
        /// </summary>
        public decimal Revenue { get; set; }

        /// <summary>
        /// Unit price the product sold at (before discount/tax), averaged across sales and
        /// weighted by quantity — same as SaleItem.UnitPrice when the catalog price didn't
        /// change within the reported range.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total IVA/tax charged on this product within the range — sum of
        /// SaleItem.TaxAmount, already included in Revenue (not on top of it).
        /// </summary>
        public decimal TaxAmount { get; set; }
    }
}
