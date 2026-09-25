namespace SecureSistem.Models
{
    /// <summary>
    /// A completed sale transaction. Totals are snapshotted at creation time (from the
    /// product prices/tax rates at that moment) so later price changes never alter history.
    /// </summary>
    public class Sale
    {
        public int Id { get; set; }

        /// <summary>
        /// Sequential, per-company folio number for tickets/reports.
        /// </summary>
        public int FolioNumber { get; set; }

        public int BranchId { get; set; }
        public int WarehouseId { get; set; }
        public int? CustomerId { get; set; }

        /// <summary>
        /// Null for a "direct" sale not tied to a till (e.g. a converted quote) — see
        /// CashSession's doc comment. Sales with a session count toward that till's
        /// expected cash on close; direct sales never do.
        /// </summary>
        public int? CashSessionId { get; set; }

        /// <summary>
        /// The cashier who rang up this sale (must match the CashSession's owner).
        /// </summary>
        public int UserId { get; set; }

        public int CompanyId { get; set; }

        /// <summary>
        /// Set when this sale came from converting a Quote — traceability only, doesn't
        /// affect anything about how the sale itself behaves.
        /// </summary>
        public int? QuoteId { get; set; }

        /// <summary>
        /// "Completed" or "Cancelled".
        /// </summary>
        public string Status { get; set; } = "Completed";

        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Branch Branch { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public Customer? Customer { get; set; }
        public CashSession? CashSession { get; set; }
        public Quote? Quote { get; set; }
        public User User { get; set; } = null!;
        public Company Company { get; set; } = null!;
        public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
