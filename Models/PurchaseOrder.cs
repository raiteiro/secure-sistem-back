namespace SecureSistem.Models
{
    /// <summary>
    /// An order placed with a supplier. Creating one never touches inventory — it's just
    /// what was requested. Receiving it (PurchaseOrdersController.Receive) is what actually
    /// adds stock, via the same InventoryStockHelper used everywhere else, tagged with this
    /// order's Supplier for traceability (InventoryMovement.SupplierId).
    /// </summary>
    public class PurchaseOrder
    {
        public int Id { get; set; }

        /// <summary>Sequential, per-company folio number.</summary>
        public int FolioNumber { get; set; }

        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }

        /// <summary>"Pending", "Received" or "Cancelled".</summary>
        public string Status { get; set; } = "Pending";

        public string? Notes { get; set; }

        public decimal Total { get; set; }

        public DateTime? ReceivedAt { get; set; }
        public string? ReceivedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Supplier Supplier { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public User User { get; set; } = null!;
        public Company Company { get; set; } = null!;
        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }
}
