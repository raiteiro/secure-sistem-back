using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Suppliers
{
    public class CreateSupplierRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TaxId { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        public bool IsConsignor { get; set; } = false;

        /// <summary>
        /// Company to create the supplier in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
