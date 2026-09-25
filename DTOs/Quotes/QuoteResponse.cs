namespace SecureSistem.DTOs.Quotes
{
    public class QuoteResponse
    {
        public int Id { get; set; }
        public int FolioNumber { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        /// <summary>"Open", "Converted" or "Cancelled".</summary>
        public string Status { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set once the quote is converted — the resulting Sale's id.</summary>
        public int? ConvertedSaleId { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
        public List<QuoteItemResponse> Items { get; set; } = new();
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
