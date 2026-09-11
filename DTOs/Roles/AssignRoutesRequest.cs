using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Roles
{
    public class AssignRoutesRequest
    {
        /// <summary>
        /// List of NavigationRoute IDs to assign to the role.
        /// Replaces all current assignments.
        /// </summary>
        [Required]
        public List<int> RouteIds { get; set; } = new();
    }
}
