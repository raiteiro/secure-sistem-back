using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
