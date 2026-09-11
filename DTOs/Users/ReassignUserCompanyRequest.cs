using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Users
{
    public class ReassignUserCompanyRequest
    {
        [Required]
        public int CompanyId { get; set; }

        /// <summary>
        /// The user's current role belongs to their old company and no longer applies,
        /// so a valid role within the target company must be provided.
        /// </summary>
        [Required]
        public int RoleId { get; set; }
    }
}
