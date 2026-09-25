using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.PurchaseOrders
{
    public class ReceivePurchaseOrderRequest
    {
        [Required, MinLength(1, ErrorMessage = "Se requiere al menos un artículo.")]
        public List<ReceivePurchaseOrderItemRequest> Items { get; set; } = new();

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
