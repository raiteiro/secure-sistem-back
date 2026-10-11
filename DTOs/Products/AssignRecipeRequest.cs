using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Products
{
    public class AssignRecipeRequest
    {
        /// <summary>
        /// The product's full recipe. Replaces the current one.
        /// </summary>
        [Required, MinLength(1, ErrorMessage = "La receta debe tener al menos un insumo.")]
        public List<RecipeItemRequest> Items { get; set; } = new();
    }
}
