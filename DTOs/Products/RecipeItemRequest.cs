using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Products
{
    public class RecipeItemRequest
    {
        [Required]
        public int SupplyProductId { get; set; }

        /// <summary>
        /// Amount of the supply needed per 1 unit of the manufactured product.
        /// </summary>
        [Required, Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad de cada insumo debe ser mayor a 0.")]
        public decimal Quantity { get; set; }
    }
}
