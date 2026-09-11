using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Roles
{
    public class UpdateRoleRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }
    }
}
