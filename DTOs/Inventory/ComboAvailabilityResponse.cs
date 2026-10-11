namespace SecureSistem.DTOs.Inventory
{
    /// <summary>
    /// How many units of a combo can be sold from one warehouse with the current stock of
    /// its components. Display-only: the sale's own stock check is what actually decides.
    /// </summary>
    public class ComboAvailabilityResponse
    {
        public int ComboProductId { get; set; }
        public string ComboProductName { get; set; } = string.Empty;
        public string? ComboProductSku { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>
        /// Minimum of ComboComponentAvailabilityResponse.CombosPossible across the combo's
        /// active components. 0 when the combo has no active components.
        /// </summary>
        public decimal AvailableQuantity { get; set; }

        public int CompanyId { get; set; }
        public List<ComboComponentAvailabilityResponse> Components { get; set; } = new();
    }
}
