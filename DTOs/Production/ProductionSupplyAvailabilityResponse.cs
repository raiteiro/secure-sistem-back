namespace SecureSistem.DTOs.Production
{
    public class ProductionSupplyAvailabilityResponse
    {
        public int SupplyProductId { get; set; }
        public string SupplyProductName { get; set; } = string.Empty;
        /// <summary>
        /// Abbreviation of the supply's unit of measure (e.g. "kg").
        /// </summary>
        public string SupplyUnit { get; set; } = string.Empty;

        public int SupplyUnitDecimalPlaces { get; set; }
        public decimal QuantityPerUnit { get; set; }

        /// <summary>
        /// Current stock of the supply at the warehouse (0 when it has no Inventory row).
        /// </summary>
        public decimal Stock { get; set; }

        /// <summary>
        /// floor(max(Stock, 0) / QuantityPerUnit), or 0 when the supply is inactive.
        /// </summary>
        public decimal UnitsPossible { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// True when this supply's UnitsPossible equals the product's MaxProducible.
        /// </summary>
        public bool IsLimiting { get; set; }
    }
}
