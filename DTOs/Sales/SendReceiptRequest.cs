using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Sales
{
    public class SendReceiptRequest
    {
        /// <summary>
        /// Optional — overrides the sale's customer email. Required if the sale has no
        /// customer attached, or the customer has no email registered.
        /// </summary>
        [EmailAddress]
        public string? Email { get; set; }
    }
}
