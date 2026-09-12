namespace SecureSistem.DTOs.Warehouses
{
    public class WarehouseResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int CompanyId { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
