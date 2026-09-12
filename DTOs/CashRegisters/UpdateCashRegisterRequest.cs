using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.CashRegisters
{
    public class UpdateCashRegisterRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int BranchId { get; set; }
    }
}
