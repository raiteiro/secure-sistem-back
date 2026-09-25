namespace SecureSistem.DTOs.Inventory
{
    public class InventoryMovementResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ResultingQuantity { get; set; }
        public string? Notes { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
