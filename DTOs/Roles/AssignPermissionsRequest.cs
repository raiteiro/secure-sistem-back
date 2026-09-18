using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Roles
{
    public class AssignPermissionsRequest
    {
        /// <summary>
        /// List of Permission IDs to assign to the role.
        /// Replaces all current assignments.
        /// </summary>
        [Required]
        public List<int> PermissionIds { get; set; } = new();
    }
}
