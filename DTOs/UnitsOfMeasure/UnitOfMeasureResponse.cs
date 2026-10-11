namespace SecureSistem.DTOs.UnitsOfMeasure
{
    public class UnitOfMeasureResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public int DecimalPlaces { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Active products, across every company, that use this unit.
        /// </summary>
        public int ProductCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
