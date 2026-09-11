using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.NavigationRoutes
{
    public class UpdateNavigationRouteRequest
    {
        public int? ParentId { get; set; }

        [Required, MaxLength(150)]
        public string WindowName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string RoutePath { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Icon { get; set; }

        [Required, MaxLength(100)]
        public string WindowId { get; set; } = string.Empty;

        public int Level { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
