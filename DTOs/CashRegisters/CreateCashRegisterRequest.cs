using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.CashRegisters
{
    public class CreateCashRegisterRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int BranchId { get; set; }

        /// <summary>
        /// Company to create the cash register in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
