using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Roles
{
    public class CreateRoleRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        /// <summary>
        /// Company to create the role in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
