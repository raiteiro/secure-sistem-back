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
        /// Must be the caller's own currently open session. Omit for a "direct" sale not
        /// tied to a till — e.g. a quote converted to a sale, invoiced/paid by transfer
        /// instead of rung up at a register. A direct sale still requires payments summing
        /// exactly to the total; it's not a credit/deferred-payment sale.
        /// </summary>
        public int? CashSessionId { get; set; }

        /// <summary>
        /// Company to create a direct sale in (no CashSessionId). Only honored for system
        /// administrators; ignored (defaults to the caller's own company) for everyone else.
        /// Has no effect when CashSessionId is set — the session's own company always wins.
        /// </summary>
        public int? CompanyId { get; set; }

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos un artículo.")]
        public List<CreateSaleItemRequest> Items { get; set; } = new();

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos un pago.")]
        public List<CreateSalePaymentRequest> Payments { get; set; } = new();
    }
}
