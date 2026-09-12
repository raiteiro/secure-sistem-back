using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Customers
{
    public class UpdateCustomerRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? TaxId { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }
    }
}
