using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Sales
{
    public class CreateSalePaymentRequest
    {
        /// <summary>
        /// "Cash", "Card" or "Other".
        /// </summary>
        [Required, RegularExpression("^(Cash|Card|Other)$",
            ErrorMessage = "Method must be 'Cash', 'Card' or 'Other'.")]
        public string Method { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
