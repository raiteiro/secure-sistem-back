using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Consignment
{
    public class CreateSettlementRequest
    {
        [Required]
        public int SupplierId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
