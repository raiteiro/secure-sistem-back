using System.ComponentModel.DataAnnotations;
using SecureSistem.DTOs.Sales;

namespace SecureSistem.DTOs.Quotes
{
    public class ConvertQuoteToSaleRequest
    {
        [Required]
        public int WarehouseId { get; set; }

        /// <summary>
        /// Omit for a direct sale not tied to a till. If set, must be the caller's own
        /// currently open session — same rule as POST /api/sales.
        /// </summary>
        public int? CashSessionId { get; set; }

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos un pago.")]
        public List<CreateSalePaymentRequest> Payments { get; set; } = new();
    }
}
