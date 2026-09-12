namespace SecureSistem.Models
{
    /// <summary>
    /// A partial or full return of items from a completed sale, processed against the
    /// current cashier's open session (not necessarily the original sale's session).
    /// Restocks inventory and records the refund; the original Sale is left untouched.
    /// </summary>
    public class Return
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public int WarehouseId { get; set; }
        public int CashSessionId { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }

        /// <summary>
        /// "Cash", "Card" or "Other" — how the refund was given back to the customer.
        /// Only "Cash" affects the processing cash session's expected amount.
        /// </summary>
        public string RefundMethod { get; set; } = string.Empty;

        public string? Reason { get; set; }

        public decimal SubtotalRefunded { get; set; }
        public decimal TaxRefunded { get; set; }
        public decimal TotalRefunded { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Sale Sale { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public CashSession CashSession { get; set; } = null!;
        public User User { get; set; } = null!;
        public Company Company { get; set; } = null!;
        public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
    }
}
