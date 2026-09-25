namespace SecureSistem.Models
{
    /// <summary>
    /// One product/quantity/expected-cost line of a PurchaseOrder.
    /// </summary>
    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ProductId { get; set; }

        public decimal Quantity { get; set; }

        /// <summary>Expected cost per unit, agreed with the supplier.</summary>
        public decimal UnitCost { get; set; }

        public decimal Total { get; set; }

        /// <summary>
        /// How much of Quantity has actually been received so far — lets a purchase order
        /// be received in more than one delivery. Equal to Quantity once fully received.
        /// </summary>
        public decimal QuantityReceived { get; set; }

        // Navigation properties
        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
