namespace SecureSistem.Models
{
    /// <summary>
    /// A non-binding price proposal for a customer. Items/prices are snapshotted like a
    /// Sale, but creating or editing a quote never touches inventory — nothing is reserved.
    /// Converting one (ISaleService) creates the real Sale and marks this "Converted".
    /// </summary>
    public class Quote
    {
        public int Id { get; set; }

        /// <summary>Sequential, per-company folio number.</summary>
        public int FolioNumber { get; set; }

        public int BranchId { get; set; }

        /// <summary>Optional — a quote can be drafted before a specific customer is attached.</summary>
        public int? CustomerId { get; set; }

        /// <summary>Who drafted the quote.</summary>
        public int UserId { get; set; }

        public int CompanyId { get; set; }

        /// <summary>"Open", "Converted" or "Cancelled".</summary>
        public string Status { get; set; } = "Open";

        /// <summary>Optional validity date — informational, not enforced by the backend.</summary>
        public DateTime? ExpiresAt { get; set; }

        public string? Notes { get; set; }

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
        public Customer? Customer { get; set; }
        public User User { get; set; } = null!;
        public Company Company { get; set; } = null!;
        public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
    }
}
