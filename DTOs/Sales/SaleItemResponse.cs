namespace SecureSistem.DTOs.Sales
{
    public class SaleItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public decimal Quantity { get; set; }

        /// <summary>
        /// The sold product's unit of measure — e.g. to validate a return's quantity.
        /// </summary>
        public string UnitAbbreviation { get; set; } = string.Empty;

        public int UnitDecimalPlaces { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRateValue { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }
}
