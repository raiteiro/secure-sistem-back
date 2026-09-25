using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.PurchaseOrders
{
    public class CreatePurchaseOrderRequest
    {
        [Required]
        public int SupplierId { get; set; }

        /// <summary>Where this order's stock will land once received.</summary>
        [Required]
        public int WarehouseId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos un artículo.")]
        public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();

        /// <summary>
        /// Company to create the order in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
