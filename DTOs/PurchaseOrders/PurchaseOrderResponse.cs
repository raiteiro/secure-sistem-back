namespace SecureSistem.DTOs.PurchaseOrders
{
    public class PurchaseOrderResponse
    {
        public int Id { get; set; }
        public int FolioNumber { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        /// <summary>"Pending" (nothing or only part received), "Received" (fully) or "Cancelled".</summary>
        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
        public decimal Total { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? ReceivedBy { get; set; }
        public List<PurchaseOrderItemResponse> Items { get; set; } = new();
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
