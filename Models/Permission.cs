namespace SecureSistem.Models
{
    /// <summary>
    /// A gate-able action in the system (e.g. "sales.cancel") — unlike NavigationRoute,
    /// this is a single global catalog shared by every company, not duplicated per company,
    /// because a permission key only means something if a controller actually enforces it;
    /// companies can't invent their own by creating a row through the API.
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }

        /// <summary>
        /// Stable machine key checked in code (e.g. "sales.cancel"). Never shown to the user.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Display label for role/permission management screens (e.g. "Cancelar venta").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// NavigationRoute.WindowId of the screen this action belongs to, so the frontend can
        /// group permissions by module when assigning them to a role.
        /// </summary>
        public string? WindowId { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When true, every active role — existing and future, in every company — has this
        /// permission granted automatically. Mirrors NavigationRoute.IsDefaultForNewRoles:
        /// the rollout starts fully open so nothing breaks, and companies selectively revoke
        /// it per role afterward, the same way they already do for whole screens.
        /// </summary>
        public bool IsDefaultForNewRoles { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
