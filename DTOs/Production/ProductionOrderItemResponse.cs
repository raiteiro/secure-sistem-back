namespace SecureSistem.DTOs.Production
{
    public class ProductionOrderItemResponse
    {
        public int SupplyProductId { get; set; }
        public string SupplyProductName { get; set; } = string.Empty;
        /// <summary>
        /// Abbreviation of the supply's unit of measure (e.g. "kg").
        /// </summary>
        public string SupplyUnit { get; set; } = string.Empty;
        public decimal QuantityPerUnit { get; set; }
        public decimal TotalQuantity { get; set; }
    }
}
