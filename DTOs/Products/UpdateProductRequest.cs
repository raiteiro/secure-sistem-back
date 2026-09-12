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

        [MaxLength(20)]
        public string? Unit { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Cost { get; set; }

        public int? CategoryId { get; set; }
        public int? TaxRateId { get; set; }
    }
}
