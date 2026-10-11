namespace SecureSistem.Models
{
    /// <summary>
    /// A unit of measure from the global catalog (shared by every company, no CompanyId).
    /// Every product has one, and every quantity of that product (stock, sales, recipes,
    /// production...) must respect its DecimalPlaces — see QuantityHelper.
    /// </summary>
    public class UnitOfMeasure
    {
        public int Id { get; set; }

        /// <summary>
        /// e.g. "Kilogramo". Unique, case-insensitive.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// e.g. "kg". Unique, case-insensitive.
        /// </summary>
        public string Abbreviation { get; set; } = string.Empty;

        /// <summary>
        /// How many decimals a quantity in this unit may have, 0 to 4 (quantity columns are
        /// decimal(18,4)). Can be raised freely, but never lowered while any product uses it.
        /// </summary>
        public int DecimalPlaces { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
