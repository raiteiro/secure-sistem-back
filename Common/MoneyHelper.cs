namespace SecureSistem.Common
{
    /// <summary>
    /// Single source of rounding for every monetary amount (line subtotals, taxes, totals,
    /// refunds, consignment splits). Money columns are decimal(18,2), so every amount is
    /// rounded to cents the moment it's computed — never left with extra decimals that the
    /// database would silently round later, which made "payments must equal the total"
    /// compare against a value (e.g. 12.7484) that no real payment could ever match.
    /// </summary>
    public static class MoneyHelper
    {
        /// <summary>
        /// Rounds to 2 decimals, half away from zero (0.005 → 0.01), the usual commercial
        /// rounding in Mexico — not .NET's default banker's rounding (0.005 → 0.00).
        /// </summary>
        public static decimal Round(decimal amount) =>
            Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Whether the amount has no more than 2 decimals, i.e. is a real amount of money.
        /// </summary>
        public static bool IsWholeCents(decimal amount) => amount == Round(amount);
    }
}
