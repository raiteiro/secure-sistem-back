namespace SecureSistem.DTOs.Reports
{
    public class SalesByPeriodItem
    {
        public DateTime Period { get; set; }
        public int SalesCount { get; set; }
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Every product sold within this period (not just a top N), same shape as
        /// GET /api/reports/top-products, ordered desc by QuantitySold.
        /// </summary>
        public List<TopProductResponse> Products { get; set; } = new();
    }
}
