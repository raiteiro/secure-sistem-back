namespace SecureSistem.DTOs.NavigationRoutes
{
    public class NavigationRouteResponse
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string WindowName { get; set; } = string.Empty;
        public string RoutePath { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string WindowId { get; set; } = string.Empty;
        public int Level { get; set; }
        public int SortOrder { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public List<NavigationRouteResponse> Children { get; set; } = new();
    }
}
