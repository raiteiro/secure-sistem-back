namespace SecureSistem.Models
{
    /// <summary>
    /// Assigns a navigation route to a role. All users with this role inherit access.
    /// </summary>
    public class RoleNavigationRoute
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int NavigationRouteId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Role Role { get; set; } = null!;
        public NavigationRoute NavigationRoute { get; set; } = null!;
    }
}
