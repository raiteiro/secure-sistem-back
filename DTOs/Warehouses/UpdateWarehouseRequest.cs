using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Warehouses
{
    public class UpdateWarehouseRequest
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
    }
}
