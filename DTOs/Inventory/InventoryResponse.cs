namespace SecureSistem.DTOs.Inventory
{
    public class InventoryResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }

        /// <summary>
        /// "Product" or "Supply" — lets the frontend split a warehouse's stock into tabs.
        /// </summary>
        public string ProductKind { get; set; } = string.Empty;

        public bool IsManufactured { get; set; }

        /// <summary>
        /// The product's unit of measure — for the input step and decimal validation.
        /// </summary>
        public string UnitAbbreviation { get; set; } = string.Empty;

        public int UnitDecimalPlaces { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal? MinStock { get; set; }
        public bool IsLowStock { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
