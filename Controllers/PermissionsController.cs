using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Permissions;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Read-only catalog of gate-able actions (e.g. "sales.cancel") and resolution of what
    /// the current user can do. The catalog itself is seeded by migrations, not created
    /// through this API — a permission key only means something once a controller enforces
    /// it, so nothing here lets you invent new ones. Assigning existing ones to a role is
    /// done from RolesController (mirrors how NavigationRoute assignment works).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : BaseApiController
    {
        private readonly ApplicationDbContext _context;

        public PermissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the full permission catalog, for role-management screens to pick from.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PermissionResponse>), 200)]
        public async Task<ActionResult<List<PermissionResponse>>> GetAll()
        {
            var permissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.WindowId).ThenBy(p => p.Name)
                .Select(p => new PermissionResponse
                {
                    Id = p.Id,
                    Key = p.Key,
                    Name = p.Name,
                    Description = p.Description,
                    WindowId = p.WindowId,
                    IsActive = p.IsActive,
                    IsDefaultForNewRoles = p.IsDefaultForNewRoles
                })
                .ToListAsync();

            return Ok(permissions);
        }

        /// <summary>
        /// Gets the permission keys the authenticated user effectively has, resolved from
        /// their role — what the frontend uses to decide which buttons to show enabled vs.
        /// behind a supervisor-override prompt. System administrators implicitly have every
        /// permission, so this returns every active key for them without checking role.
        /// </summary>
        [HttpGet("my-permissions")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<ActionResult<List<string>>> GetMyPermissions()
        {
            if (IsSystemAdmin())
            {
                var allKeys = await _context.Permissions
                    .Where(p => p.IsActive)
                    .Select(p => p.Key)
                    .ToListAsync();
                return Ok(allKeys);
            }

            var keys = await PermissionHelper.GetPermissionKeysAsync(_context, GetRoleId());
            return Ok(keys);
        }
    }
}
