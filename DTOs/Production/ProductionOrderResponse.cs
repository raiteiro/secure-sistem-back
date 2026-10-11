namespace SecureSistem.DTOs.Production
{
    public class ProductionOrderResponse
    {
        public int Id { get; set; }
        public int FolioNumber { get; set; }

        /// <summary>
        /// "Production" or "Disassembly".
        /// </summary>
        public string Type { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
        public List<ProductionOrderItemResponse> Items { get; set; } = new();
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
