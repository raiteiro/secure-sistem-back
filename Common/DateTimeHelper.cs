namespace SecureSistem.Common
{
    /// <summary>
    /// Single source of "now" for every business-facing timestamp (CreatedAt, ModifiedAt,
    /// OpenedAt, ClosedAt, LastLoginAt, etc.) — always Mexico City wall-clock time,
    /// regardless of the server's own time zone. This is deliberately NOT used for
    /// token/session expiration (see ITokenService, RefreshToken, PasswordResetToken),
    /// which stays on DateTime.UtcNow so issuance and expiry checks share one clock.
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo MexicoTimeZone = ResolveMexicoTimeZone();

        /// <summary>
        /// Current Mexico City time as a naive (Kind=Unspecified) value — the API returns
        /// it as-is, with no "Z"/offset suffix, so the frontend can display it directly
        /// without any timezone conversion of its own.
        /// </summary>
        public static DateTime Now =>
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MexicoTimeZone), DateTimeKind.Unspecified);

        private static TimeZoneInfo ResolveMexicoTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
            }
        }
    }
}
