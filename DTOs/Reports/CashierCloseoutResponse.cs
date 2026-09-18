namespace SecureSistem.DTOs.Reports
{
    public class CashierCloseoutResponse
    {
        public int CashSessionId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int CashRegisterId { get; set; }
        public string CashRegisterName { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int SalesCount { get; set; }
        public decimal OpeningAmount { get; set; }
        public decimal CashSalesTotal { get; set; }
        public decimal CardSalesTotal { get; set; }
        public decimal OtherSalesTotal { get; set; }
        public decimal RefundsTotal { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? ClosingAmount { get; set; }
        public decimal? Difference { get; set; }
    }
}
