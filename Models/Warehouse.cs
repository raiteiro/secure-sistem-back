namespace SecureSistem.Models
{
    /// <summary>
    /// Represents a stock location. May optionally belong to a specific branch (a
    /// storeroom for that point of sale), or stand independent of any branch (a
    /// central warehouse that supplies multiple branches but doesn't sell directly).
    /// </summary>
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }

        public int CompanyId { get; set; }

        /// <summary>
        /// Branch this warehouse belongs to. Null means it's an independent/central
        /// warehouse not tied to a specific point of sale.
        /// </summary>
        public int? BranchId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public Branch? Branch { get; set; }
    }
}
