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
            ErrorMessage = "ColorPreset debe ser uno de los ids de paleta conocidos.")]
        public string? ColorPreset { get; set; }
    }
}
