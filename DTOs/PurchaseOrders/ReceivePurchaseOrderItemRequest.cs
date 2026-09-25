using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.PurchaseOrders
{
    public class ReceivePurchaseOrderItemRequest
    {
        [Required]
        public int PurchaseOrderItemId { get; set; }

        /// <summary>
        /// How much of this line to receive now — can be less than what's still pending
        /// (a delivery split across more than one shipment).
        /// </summary>
        [Required, Range(0.0001, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
