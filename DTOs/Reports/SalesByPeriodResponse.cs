namespace SecureSistem.DTOs.Reports
{
    public class SalesByPeriodResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string GroupBy { get; set; } = string.Empty;
        public List<SalesByPeriodItem> Periods { get; set; } = new();
        public int TotalSalesCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
