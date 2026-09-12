using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.CashSessions
{
    public class OpenCashSessionRequest
    {
        [Required]
        public int CashRegisterId { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal OpeningAmount { get; set; }
    }
}
