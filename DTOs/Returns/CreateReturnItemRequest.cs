using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Returns
{
    public class CreateReturnItemRequest
    {
        /// <summary>
        /// The SaleItem (line) of the original sale being returned.
        /// </summary>
        [Required]
        public int SaleItemId { get; set; }

        [Required, Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
