namespace SecureSistem.Models
{
    /// <summary>
    /// A single cashier shift on a cash register: opened with a starting cash count,
    /// eventually closed with a final count. While ClosedAt is null, the session is open
    /// and sales can be rung up against it.
    /// </summary>
    public class CashSession
    {
        public int Id { get; set; }

        public int CashRegisterId { get; set; }

        /// <summary>
        /// The cashier who opened this session.
        /// </summary>
        public int UserId { get; set; }

        public int CompanyId { get; set; }

        public decimal OpeningAmount { get; set; }
        public DateTime OpenedAt { get; set; }

        /// <summary>
        /// Cash actually counted at close time.
        /// </summary>
        public decimal? ClosingAmount { get; set; }

        /// <summary>
        /// Opening amount plus cash sales recorded during the session. Null until closed.
        /// </summary>
        public decimal? ExpectedAmount { get; set; }

        /// <summary>
        /// ClosingAmount minus ExpectedAmount. Positive = surplus, negative = shortage.
        /// </summary>
        public decimal? Difference { get; set; }

        public DateTime? ClosedAt { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public CashRegister CashRegister { get; set; } = null!;
        public User User { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
