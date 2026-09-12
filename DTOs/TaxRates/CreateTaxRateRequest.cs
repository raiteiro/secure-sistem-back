using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.TaxRates
{
    public class CreateTaxRateRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tax rate as a fraction (e.g. 0.16 for 16%).
        /// </summary>
        [Required, Range(0, 1)]
        public decimal Rate { get; set; }

        /// <summary>
        /// Company to create the tax rate in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
