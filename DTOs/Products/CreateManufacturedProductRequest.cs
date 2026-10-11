using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Products
{
    /// <summary>
    /// A manufactured product plus its recipe, created together in one transaction.
    /// </summary>
    public class CreateManufacturedProductRequest
    {
        [MaxLength(100)]
        public string? Sku { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Unit of measure from the global catalog (GET /api/unitsofmeasure). Must be active.
        /// </summary>
        [Required(ErrorMessage = "La unidad de medida es obligatoria."), Range(1, int.MaxValue, ErrorMessage = "La unidad de medida es obligatoria.")]
        public int UnitId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Cost { get; set; }

        public int? CategoryId { get; set; }
        public int? TaxRateId { get; set; }

        /// <summary>
        /// Company to create the product in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }

        [Required, MinLength(1, ErrorMessage = "La receta debe tener al menos un insumo.")]
        public List<RecipeItemRequest> Recipe { get; set; } = new();
    }
}
