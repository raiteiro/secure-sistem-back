namespace SecureSistem.DTOs.Production
{
    /// <summary>
    /// How many units of a manufactured product can be made at one warehouse with the
    /// current stock of its supplies. Display-only: the production run's own stock check
    /// is what actually decides.
    /// </summary>
    public class ProductionAvailabilityResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public string ProductUnitAbbreviation { get; set; } = string.Empty;
        public int ProductUnitDecimalPlaces { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>
        /// Stock of the manufactured product itself at the warehouse — the most that can be
        /// disassembled.
        /// </summary>
        public decimal CurrentStock { get; set; }

        /// <summary>
        /// Minimum of UnitsPossible across the recipe's supplies; 0 without an active recipe.
        /// </summary>
        public decimal MaxProducible { get; set; }

        public int CompanyId { get; set; }
        public List<ProductionSupplyAvailabilityResponse> Supplies { get; set; } = new();
    }
}
