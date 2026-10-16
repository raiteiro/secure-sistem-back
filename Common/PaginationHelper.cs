using SecureSistem.DTOs.Common;

namespace SecureSistem.Common
{
    /// <summary>
    /// Shared page/pageSize normalization and envelope-building for list endpoints.
    /// Deliberately doesn't force a mapping style — Skip/Take stay valid on an IQueryable
    /// whether you apply them before or after a .Select() projection, so each controller
    /// keeps whatever mapping pattern (in-memory MapToResponse vs. SQL projection) it
    /// already used before pagination existed.
    /// </summary>
    public static class PaginationHelper
    {
        public const int DefaultPageSize = 25;
        public const int MaxPageSize = 100;

        public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
        {
            var normalizedPage = page is null or < 1 ? 1 : page.Value;
            var normalizedPageSize = pageSize is null or < 1 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);
            return (normalizedPage, normalizedPageSize);
        }

        /// <summary>
        /// Pages an already-filtered/ordered query. Materializes it first and pages the
        /// resulting list in memory rather than translating Skip()/Take() to SQL
        /// OFFSET/FETCH — the production SQL Server for this app is SQL Server 2008
        /// SP3, which predates that syntax (added in SQL Server 2012) and throws
        /// "Incorrect syntax near 'OFFSET'" otherwise. See TECH_DEBT.md.
        /// </summary>
        public static IQueryable<T> ApplyPage<T>(this IQueryable<T> query, int page, int pageSize) =>
            query.ToList().Skip((page - 1) * pageSize).Take(pageSize).AsQueryable();

        public static PagedResponse<T> ToPagedResponse<T>(this List<T> items, int page, int pageSize, int totalCount) =>
            new()
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
    }
}
