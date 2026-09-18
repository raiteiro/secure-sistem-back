namespace SecureSistem.DTOs.Sales
{
    /// <summary>
    /// Print-ready representation of a sale: the full SaleResponse plus the company header
    /// (name, tax id, address, phone, logo) a receipt needs that SaleResponse doesn't carry.
    /// </summary>
    public class ReceiptResponse
    {
        public int SaleId { get; set; }
        public int FolioNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyTaxId { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyPhone { get; set; }
        public string? CompanyLogoPath { get; set; }

        public string BranchName { get; set; } = string.Empty;
        public string CashierUsername { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }

        public List<SaleItemResponse> Items { get; set; } = new();
        public List<PaymentResponse> Payments { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
    }
}
