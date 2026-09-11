using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.NavigationRoutes
{
    public class CreateNavigationRouteRequest
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

        /// <summary>
        /// Company to create the route in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
