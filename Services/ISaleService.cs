namespace SecureSistem.Services
{
    /// <summary>
    /// Core sale-creation logic shared by SalesController (POS/direct sales) and
    /// QuotesController (converting a Quote into a real Sale).
    /// </summary>
    public interface ISaleService
    {
        /// <summary>
        /// Validates branch/warehouse/customer/products, snapshots prices/taxes, deducts
        /// stock (including combo components and consignor attribution), and persists the
        /// Sale — all in one transaction. Returns the new Sale's Id, or a Spanish error
        /// message if validation failed (never throws for that case).
        /// </summary>
        Task<(int? SaleId, string? Error)> CreateSaleAsync(SaleCreationRequest request);
    }
}
