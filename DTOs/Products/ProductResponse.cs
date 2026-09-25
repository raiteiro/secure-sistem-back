namespace SecureSistem.DTOs.Products
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string? Sku { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public decimal? Cost { get; set; }
        public string? ImagePath { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? TaxRateId { get; set; }
        public string? TaxRateName { get; set; }
        public decimal? TaxRateValue { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; }
        public bool IsCombo { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public bool SupplierIsConsignor { get; set; }
        public string? CommissionType { get; set; }
        public decimal? CommissionValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
