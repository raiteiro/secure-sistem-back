namespace SecureSistem.DTOs.Common
{
    /// <summary>
    /// Envelope for paginated list endpoints. See CLAUDE.md → "Paginación en listados" for
    /// which endpoints need this and why.
    /// </summary>
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
