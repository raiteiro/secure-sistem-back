namespace SecureSistem.Models
{
    /// <summary>
    /// A record of one production run ("Production": supplies in, product out) or one
    /// disassembly ("Disassembly": product in, supplies back) at a warehouse. Its Items
    /// snapshot the recipe at the time, so history doesn't change if the recipe is edited.
    /// </summary>
    public class ProductionOrder
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        /// <summary>
        /// Consecutive per company, shared by both types.
        /// </summary>
        public int FolioNumber { get; set; }

        /// <summary>
        /// "Production" or "Disassembly".
        /// </summary>
        public string Type { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }

        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Company Company { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public User User { get; set; } = null!;
        public ICollection<ProductionOrderItem> Items { get; set; } = new List<ProductionOrderItem>();
    }
}
