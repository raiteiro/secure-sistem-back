namespace SecureSistem.Models
{
    /// <summary>
    /// Grants a permission to a role. All users with this role inherit it. Mirrors
    /// RoleNavigationRoute's shape/semantics, one level below "can see this screen":
    /// "can perform this specific action within it".
    /// </summary>
    public class RolePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public Role Role { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
    }
}
