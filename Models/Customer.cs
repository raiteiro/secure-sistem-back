namespace SecureSistem.Models
{
    /// <summary>
    /// A customer that can optionally be attached to a sale. Sales can also be made to
    /// "público en general" (walk-in) without a registered customer.
    /// </summary>
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }

        /// <summary>
        /// Tax identification number (RFC, NIT, etc.), for future invoicing.
        /// </summary>
        public string? TaxId { get; set; }

        public string? Address { get; set; }

        public int CompanyId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
    }
}
