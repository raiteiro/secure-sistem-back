using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Products
{
    public class CreateProductRequest
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
        /// True if this product is a combo/kit — see POST /api/products/{id}/combo-items to
        /// define what it's made of once created.
        /// </summary>
        public bool IsCombo { get; set; } = false;

        /// <summary>
        /// "Product" (sellable, the default) or "Supply" (insumo: bought and consumed by
        /// production, never sold). Manufactured products are created via
        /// POST /api/products/manufactured instead.
        /// </summary>
        [RegularExpression("^(Product|Supply)$", ErrorMessage = "El tipo debe ser 'Product' o 'Supply'.")]
        public string Kind { get; set; } = "Product";

        /// <summary>
        /// Primary/consignor supplier. Required (along with CommissionType/CommissionValue)
        /// when the supplier is a consignor — see Product.CommissionType doc comment.
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>"Percentage" or "FixedAmount" — only when SupplierId is a consignor.</summary>
        [RegularExpression("^(Percentage|FixedAmount)$", ErrorMessage = "El tipo de comisión debe ser 'Percentage' o 'FixedAmount'.")]
        public string? CommissionType { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CommissionValue { get; set; }

        /// <summary>
        /// Company to create the product in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
