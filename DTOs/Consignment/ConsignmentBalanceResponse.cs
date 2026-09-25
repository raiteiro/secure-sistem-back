namespace SecureSistem.DTOs.Consignment
{
    public class ConsignmentBalanceResponse
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int PendingSalesCount { get; set; }
        public decimal PendingAmount { get; set; }
    }
}
