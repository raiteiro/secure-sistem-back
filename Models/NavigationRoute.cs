namespace SecureSistem.Models
{
    /// <summary>
    /// Represents a navigation menu route entry.
    /// </summary>
    public class NavigationRoute
    {
        public int Id { get; set; }

        /// <summary>
        /// Parent route ID for hierarchical menu structure. Null if top-level.
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Display name of the window/menu item.
        /// </summary>
        public string WindowName { get; set; } = string.Empty;

        /// <summary>
        /// URL or route path to navigate to.
        /// </summary>
        public string RoutePath { get; set; } = string.Empty;

        /// <summary>
        /// Icon identifier (CSS class, icon name, etc.).
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Unique window identifier.
        /// </summary>
        public string WindowId { get; set; } = string.Empty;

        /// <summary>
        /// Hierarchy level: 0 = parent/group, 1+ = child window.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Display order within the same level/parent.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Company ID for multi-tenant support.
        /// </summary>
        public int CompanyId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation properties
        public NavigationRoute? Parent { get; set; }
        public ICollection<NavigationRoute> Children { get; set; } = new List<NavigationRoute>();
        public Company Company { get; set; } = null!;
    }
}
