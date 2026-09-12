using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Sales
{
    public class CreateSaleRequest
    {
        [Required]
        public int BranchId { get; set; }

        /// <summary>
        /// Which warehouse to deduct stock from for this sale's items.
        /// </summary>
        [Required]
        public int WarehouseId { get; set; }

        /// <summary>
        /// Optional — omit for a walk-in "público en general" sale.
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Must be the caller's own currently open session.
        /// </summary>
        [Required]
        public int CashSessionId { get; set; }

        [Required, MinLength(1, ErrorMessage = "At least one item is required.")]
        public List<CreateSaleItemRequest> Items { get; set; } = new();

        [Required, MinLength(1, ErrorMessage = "At least one payment is required.")]
        public List<CreateSalePaymentRequest> Payments { get; set; } = new();
    }
}
