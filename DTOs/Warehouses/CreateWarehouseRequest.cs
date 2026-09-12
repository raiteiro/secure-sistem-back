using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Warehouses
{
    public class CreateWarehouseRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        /// <summary>
        /// Branch this warehouse belongs to. Null means it's an independent/central
        /// warehouse not tied to a specific point of sale.
        /// </summary>
        public int? BranchId { get; set; }

        /// <summary>
        /// Company to create the warehouse in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
