namespace SecureSistem.Services
{
    /// <summary>
    /// One cart line for ISaleService.CreateSaleAsync. UnitPriceOverride exists only for
    /// QuotesController's convert-to-sale (locks in the price that was actually quoted,
    /// even if the product's catalog price changed since) — the public POST /api/sales
    /// endpoint never sets it, always pricing from the product's current catalog price, so
    /// a cashier can't manipulate it through the API.
    /// </summary>
    public class SaleLineItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal? UnitPriceOverride { get; set; }
    }
}
