namespace SecureSistem.Models
{
    /// <summary>
    /// One supply line of a ProductionOrder: a copy of the recipe line at the time of the
    /// operation, plus the total amount consumed (production) or returned (disassembly).
    /// </summary>
    public class ProductionOrderItem
    {
        public int Id { get; set; }
        public int ProductionOrderId { get; set; }
        public int SupplyProductId { get; set; }

        /// <summary>
        /// Recipe quantity per 1 unit of product, as it was when the order was made.
        /// </summary>
        public decimal QuantityPerUnit { get; set; }

        /// <summary>
        /// QuantityPerUnit × ProductionOrder.Quantity.
        /// </summary>
        public decimal TotalQuantity { get; set; }

        // Navigation properties
        public ProductionOrder ProductionOrder { get; set; } = null!;
        public Product SupplyProduct { get; set; } = null!;
    }
}
