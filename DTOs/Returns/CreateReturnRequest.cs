using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Returns
{
    public class CreateReturnRequest
    {
        [Required]
        public int SaleId { get; set; }

        /// <summary>
        /// Must be the caller's own currently open session — the return is processed
        /// (and refunded) against it, regardless of which session the original sale used.
        /// </summary>
        [Required]
        public int CashSessionId { get; set; }

        /// <summary>
        /// "Cash", "Card" or "Other". Only "Cash" affects the processing session's
        /// expected cash amount.
        /// </summary>
        [Required, RegularExpression("^(Cash|Card|Other)$",
            ErrorMessage = "RefundMethod must be 'Cash', 'Card' or 'Other'.")]
        public string RefundMethod { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required, MinLength(1, ErrorMessage = "At least one item is required.")]
        public List<CreateReturnItemRequest> Items { get; set; } = new();
    }
}
