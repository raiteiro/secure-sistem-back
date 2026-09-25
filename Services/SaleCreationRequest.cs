using SecureSistem.DTOs.Sales;

namespace SecureSistem.Services
{
    /// <summary>
    /// Pre-validated inputs for ISaleService.CreateSaleAsync — company/session ownership
    /// checks happen in the calling controller (they differ by context: a POS sale checks
    /// the caller's own open CashSession, a direct sale just needs the caller's company),
    /// everything past that (products, stock, tax/commission math, persistence) is shared.
    /// </summary>
    public class SaleCreationRequest
    {
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public int WarehouseId { get; set; }
        public int? CustomerId { get; set; }

        /// <summary>Null for a direct sale not tied to a till.</summary>
        public int? CashSessionId { get; set; }

        /// <summary>Set when this sale is being created by converting a Quote.</summary>
        public int? QuoteId { get; set; }

        public int UserId { get; set; }
        public string CurrentUser { get; set; } = string.Empty;
        public List<SaleLineItem> Items { get; set; } = new();
        public List<CreateSalePaymentRequest> Payments { get; set; } = new();
    }
}
