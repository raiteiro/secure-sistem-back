using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Branches
{
    public class CreateBranchRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// Company to create the branch in. Only honored for system administrators;
        /// ignored (defaults to the caller's own company) for everyone else.
        /// </summary>
        public int? CompanyId { get; set; }
    }
}
