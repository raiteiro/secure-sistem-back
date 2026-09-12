namespace SecureSistem.Models
{
    /// <summary>
    /// Immutable ledger entry recording a stock change for a product at a warehouse.
    /// Inventory.Quantity is only ever changed as a side effect of inserting one of these.
    /// </summary>
    public class InventoryMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int CompanyId { get; set; }

        /// <summary>
        /// "In", "Out", "Adjustment", "Purchase", "Sale" or "Return". "Purchase" behaves like
        /// "In" (increases stock) and "Sale"/"Return" behave like "Out"/"In" respectively
        /// (a "Return" restocks — it's what a cancelled sale or a customer return records) —
        /// they exist only to record the movement's business cause more precisely.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Signed change in stock: positive increases it, negative decreases it.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Snapshot of the resulting stock quantity right after this movement was applied,
        /// so the ledger can be audited without replaying the whole history.
        /// </summary>
        public decimal ResultingQuantity { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
