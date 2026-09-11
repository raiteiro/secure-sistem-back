using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Branches
{
    public class UpdateBranchRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }
    }
}
