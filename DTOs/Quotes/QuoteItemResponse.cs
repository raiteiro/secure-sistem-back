namespace SecureSistem.DTOs.Quotes
{
    public class QuoteItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRateValue { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
