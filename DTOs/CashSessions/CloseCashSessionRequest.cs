using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.CashSessions
{
    public class CloseCashSessionRequest
    {
        [Required, Range(0, double.MaxValue)]
        public decimal ClosingAmount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
