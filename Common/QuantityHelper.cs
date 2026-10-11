using SecureSistem.Models;

namespace SecureSistem.Common
{
    /// <summary>
    /// Single source of the "a quantity must fit its product's unit of measure" rule: a
    /// product in "Pieza" (0 decimals) only takes whole quantities, one in "Kilogramo"
    /// (3 decimals) takes up to 3, etc. Applied to every quantity the user types in (sales,
    /// quotes, returns, stock movements, purchase orders, recipes, combos, production).
    /// Derived quantities (e.g. 0.5 kg × 3 consumed by a production run) are the result of
    /// a multiplication and are not validated with it.
    /// </summary>
    public static class QuantityHelper
    {
        /// <summary>
        /// Returns null if the quantity fits the unit's DecimalPlaces, or a Spanish error
        /// message naming the product otherwise. The sign is ignored.
        /// </summary>
        public static string? ValidateDecimals(decimal quantity, UnitOfMeasure unit, string productName)
        {
            if (FitsDecimals(quantity, unit.DecimalPlaces))
                return null;

            return unit.DecimalPlaces == 0
                ? $"'{productName}' se maneja en {unit.Name} solo en cantidades enteras."
                : $"'{productName}' se maneja en {unit.Name} con máximo {unit.DecimalPlaces} decimales.";
        }

        /// <summary>
        /// Whether the quantity has no more than the given number of decimals.
        /// </summary>
        public static bool FitsDecimals(decimal quantity, int decimalPlaces) =>
            quantity == Math.Round(quantity, decimalPlaces);
    }
}
