namespace SecureSistem.DTOs.Returns
{
    public class ReturnResponse
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int SaleFolioNumber { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int CashSessionId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RefundMethod { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public decimal SubtotalRefunded { get; set; }
        public decimal TaxRefunded { get; set; }
        public decimal TotalRefunded { get; set; }
        public List<ReturnItemResponse> Items { get; set; } = new();
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
