using System.ComponentModel.DataAnnotations;

namespace SecureSistem.DTOs.UnitsOfMeasure
{
    /// <summary>
    /// Body for both creating and updating a unit of measure.
    /// </summary>
    public class UnitOfMeasureRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio."), MaxLength(50, ErrorMessage = "El nombre admite máximo 50 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La abreviatura es obligatoria."), MaxLength(10, ErrorMessage = "La abreviatura admite máximo 10 caracteres.")]
        public string Abbreviation { get; set; } = string.Empty;

        [Range(0, 4, ErrorMessage = "Los decimales deben estar entre 0 y 4.")]
        public int DecimalPlaces { get; set; }
    }
}
