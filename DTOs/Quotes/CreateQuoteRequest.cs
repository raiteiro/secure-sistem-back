using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Quotes
{
    public class CreateQuoteRequest
    {
        [Required]
        public int BranchId { get; set; }

        /// <summary>Optional — a quote can be drafted before a specific customer is attached.</summary>
        public int? CustomerId { get; set; }

        /// <summary>Optional validity date — informational, not enforced automatically.</summary>
        public DateTime? ExpiresAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required, MinLength(1, ErrorMessage = "Se requiere al menos un artículo.")]
        public List<CreateQuoteItemRequest> Items { get; set; } = new();

        /// <summary>
        /// Company to create the quote in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
