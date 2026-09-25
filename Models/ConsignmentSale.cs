namespace SecureSistem.Models
{
    /// <summary>
    /// Auto-created whenever a SaleItem is rung up for a product attributed to a consignor
    /// supplier (Product.SupplierId set, Supplier.IsConsignor). Snapshots the sale/commission
    /// split at sale time — like SaleItem itself — so later changes to the product's
    /// commission config never alter history. Stays unsettled (SettlementId null) until paid
    /// out via a ConsignmentSettlement.
    /// </summary>
    public class ConsignmentSale
    {
        public int Id { get; set; }
        public int SaleItemId { get; set; }
        public int ProductId { get; set; }
        public int SupplierId { get; set; }
        public int CompanyId { get; set; }

        public decimal Quantity { get; set; }

        /// <summary>Line amount actually sold (may shrink if partially returned later).</summary>
        public decimal SaleAmount { get; set; }

        /// <summary>Amount owed to the consignor.</summary>
        public decimal ConsignorAmount { get; set; }

        /// <summary>Amount the store keeps.</summary>
        public decimal StoreAmount { get; set; }

        /// <summary>
        /// True if the originating sale was cancelled — the amounts are left as a historical
        /// record but excluded from pending-balance calculations, same idea as Sale.Status.
        /// </summary>
        public bool IsVoided { get; set; } = false;

        public int? SettlementId { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public SaleItem SaleItem { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public Supplier Supplier { get; set; } = null!;
        public ConsignmentSettlement? Settlement { get; set; }
        public Company Company { get; set; } = null!;
    }
}
