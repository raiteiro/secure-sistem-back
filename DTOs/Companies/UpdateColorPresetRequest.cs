using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.Companies
{
    public class UpdateColorPresetRequest
    {
        /// <summary>
        /// Id of the curated color palette for this company's theme (e.g. "ocean", "emerald").
        /// Null uses the default ("purple") look.
        /// </summary>
        [RegularExpression("^(purple|ocean|emerald|teal|ruby|amber|rose|indigo|slate|sky-light)$",
            ErrorMessage = "ColorPreset must be one of the known palette ids.")]
        public string? ColorPreset { get; set; }
    }
}
