namespace SecureSistem.Models
{
    /// <summary>
    /// Assigns a navigation route directly to a specific user.
    /// </summary>
    public class UserNavigationRoute
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int NavigationRouteId { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public User User { get; set; } = null!;
        public NavigationRoute NavigationRoute { get; set; } = null!;
    }
}
