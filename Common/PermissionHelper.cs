using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;

namespace SecureSistem.Common
{
    /// <summary>
    /// Resolves whether a role has a given Permission — the single source of truth used both
    /// by gated actions (e.g. SalesController.Cancel) and by GET /api/permissions/my-permissions.
    /// </summary>
    public static class PermissionHelper
    {
        public static Task<bool> HasPermissionAsync(ApplicationDbContext context, int roleId, string key) =>
            context.RolePermissions.AnyAsync(rp =>
                rp.RoleId == roleId && rp.IsActive &&
                rp.Permission.Key == key && rp.Permission.IsActive);

        public static Task<List<string>> GetPermissionKeysAsync(ApplicationDbContext context, int roleId) =>
            context.RolePermissions
                .Where(rp => rp.RoleId == roleId && rp.IsActive && rp.Permission.IsActive)
                .Select(rp => rp.Permission.Key)
                .ToListAsync();
    }
}
