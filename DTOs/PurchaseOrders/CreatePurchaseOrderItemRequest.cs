using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.PurchaseOrders
{
    public class CreatePurchaseOrderItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required, Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; }
    }
}
