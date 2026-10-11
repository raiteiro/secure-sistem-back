namespace SecureSistem.Common
{
    /// <summary>
    /// Shared "how many units can be built from these parts" rule, used both for combos
    /// (InventoryController.GetComboAvailability: components per combo) and for manufactured
    /// products (ProductionController.GetAvailability: supplies per unit). Kept in one place
    /// so both always agree with what the stock check in a sale / production run accepts.
    /// </summary>
    public static class ComponentAvailabilityHelper
    {
        /// <summary>
        /// floor(max(stock, 0) / quantityPerUnit), or 0 when the part is inactive (the sale /
        /// production run rejects it outright) or quantityPerUnit isn't positive.
        /// </summary>
        public static decimal UnitsPossible(decimal stock, decimal quantityPerUnit, bool isActive) =>
            isActive && quantityPerUnit > 0
                ? Math.Floor(Math.Max(stock, 0m) / quantityPerUnit)
                : 0m;

        /// <summary>
        /// The minimum across all parts, or 0 when there are no active parts at all (nothing
        /// would be deducted, so it must never be offered).
        /// </summary>
        public static decimal MaxUnits(IReadOnlyCollection<decimal> unitsPossiblePerPart) =>
            unitsPossiblePerPart.Count == 0 ? 0m : unitsPossiblePerPart.Min();
    }
}
