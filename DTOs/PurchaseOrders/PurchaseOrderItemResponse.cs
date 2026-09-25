namespace SecureSistem.DTOs.PurchaseOrders
{
    public class PurchaseOrderItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Total { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal QuantityPending { get; set; }
    }
}
