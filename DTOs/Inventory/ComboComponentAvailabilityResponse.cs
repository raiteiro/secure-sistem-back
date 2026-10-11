namespace SecureSistem.DTOs.Inventory
{
    public class ComboComponentAvailabilityResponse
    {
        public int ComponentProductId { get; set; }
        public string ComponentProductName { get; set; } = string.Empty;
        public decimal QuantityPerCombo { get; set; }

        /// <summary>
        /// Current stock of the component at the warehouse (0 when it has no Inventory row).
        /// </summary>
        public decimal Stock { get; set; }

        /// <summary>
        /// floor(max(Stock, 0) / QuantityPerCombo), or 0 when the component is inactive.
        /// </summary>
        public decimal CombosPossible { get; set; }

        public bool IsActive { get; set; }

        /// <summary>
        /// True when this component's CombosPossible equals the combo's AvailableQuantity,
        /// i.e. it's (one of) the component(s) capping how many combos can be sold.
        /// </summary>
        public bool IsLimiting { get; set; }
    }
}
