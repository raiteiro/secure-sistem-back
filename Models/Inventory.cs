namespace SecureSistem.Models
{
    /// <summary>
    /// Current stock level of a product at a specific warehouse. Changes only ever happen
    /// as a side effect of recording an InventoryMovement, never edited directly (except for
    /// the low-stock threshold), so it always stays consistent with the movement ledger.
    /// </summary>
    public class Inventory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int CompanyId { get; set; }

        public decimal Quantity { get; set; }

        /// <summary>
        /// Low-stock alert threshold for this product at this warehouse. Null means no alert.
        /// </summary>
        public decimal? MinStock { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
