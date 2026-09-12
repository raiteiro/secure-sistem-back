namespace SecureSistem.Models
{
    /// <summary>
    /// One payment applied to a sale. A sale can have more than one (mixed payment,
    /// e.g. part cash + part card).
    /// </summary>
    public class Payment
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int CompanyId { get; set; }

        /// <summary>
        /// "Cash", "Card" or "Other".
        /// </summary>
        public string Method { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Sale Sale { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
