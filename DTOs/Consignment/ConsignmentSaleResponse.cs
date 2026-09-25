namespace SecureSistem.DTOs.Consignment
{
    public class ConsignmentSaleResponse
    {
        public int Id { get; set; }
        public int SaleItemId { get; set; }
        public int SaleId { get; set; }
        public int SaleFolioNumber { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal SaleAmount { get; set; }
        public decimal ConsignorAmount { get; set; }
        public decimal StoreAmount { get; set; }
        public bool IsVoided { get; set; }
        public int? SettlementId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
