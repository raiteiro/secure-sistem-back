using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.TaxRates
{
    public class UpdateTaxRateRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, Range(0, 1)]
        public decimal Rate { get; set; }
    }
}
