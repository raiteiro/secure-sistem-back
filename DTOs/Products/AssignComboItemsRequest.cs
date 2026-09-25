using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Products
{
    public class AssignComboItemsRequest
    {
        /// <summary>
        /// The combo's full component list. Replaces all current components.
        /// </summary>
        [Required, MinLength(1)]
        public List<ComboItemRequest> Items { get; set; } = new();
    }
}
