using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Products
{
    public class UpdateProductRequest
    {
        [MaxLength(100)]
        public string? Sku { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Unit of measure from the global catalog (GET /api/unitsofmeasure). Must be active,
        /// or the product's current unit even if it was deactivated since.
        /// </summary>
        [Required(ErrorMessage = "La unidad de medida es obligatoria."), Range(1, int.MaxValue, ErrorMessage = "La unidad de medida es obligatoria.")]
        public int UnitId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Cost { get; set; }

        public int? CategoryId { get; set; }
        public int? TaxRateId { get; set; }

        public bool IsCombo { get; set; } = false;

        /// <summary>
        /// "Product" or "Supply". Omit (null) to keep the current kind, so an update that
        /// doesn't know about it never silently turns a supply into a product.
        /// </summary>
        [RegularExpression("^(Product|Supply)$", ErrorMessage = "El tipo debe ser 'Product' o 'Supply'.")]
        public string? Kind { get; set; }

        public int? SupplierId { get; set; }

        [RegularExpression("^(Percentage|FixedAmount)$", ErrorMessage = "El tipo de comisión debe ser 'Percentage' o 'FixedAmount'.")]
        public string? CommissionType { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CommissionValue { get; set; }
    }
}
