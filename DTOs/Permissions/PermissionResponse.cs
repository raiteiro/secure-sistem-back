namespace SecureSistem.DTOs.Permissions
{
    public class PermissionResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? WindowId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefaultForNewRoles { get; set; }
    }
}
