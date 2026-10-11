using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Production
{
    /// <summary>
    /// Body for both producing (POST /api/production) and disassembling
    /// (POST /api/production/disassemble) a manufactured product at a warehouse.
    /// </summary>
    public class CreateProductionRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        /// <summary>
        /// Units of the product to make or take apart. Fractions are allowed, like every
        /// other inventory quantity.
        /// </summary>
        [Required, Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public decimal Quantity { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
