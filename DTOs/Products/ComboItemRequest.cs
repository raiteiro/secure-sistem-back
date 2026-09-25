using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Products
{
    public class ComboItemRequest
    {
        [Required]
        public int ComponentProductId { get; set; }

        [Required, Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
