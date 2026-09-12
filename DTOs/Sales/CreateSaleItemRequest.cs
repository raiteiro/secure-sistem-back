using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Sales
{
    public class CreateSaleItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required, Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;
    }
}
