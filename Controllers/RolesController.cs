using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.Roles;
using SecureSistem.DTOs.NavigationRoutes;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for role management and route assignment.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RolesController> _logger;

        public RolesController(ApplicationDbContext context, ILogger<RolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active roles for the authenticated user's company.
        /// System administrators see roles across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<RoleResponse>), 200)]
        public async Task<ActionResult<List<RoleResponse>>> GetAll()
        {
            var query = _context.Roles.Where(r => r.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var roles = await query
                .OrderBy(r => r.Name)
                .ToListAsync();

            var roleIds = roles.Select(r => r.Id).ToList();

            // Get all route assignments for these roles in one query
            var routeAssignments = await _context.RoleNavigationRoutes
                .Where(rr => roleIds.Contains(rr.RoleId) && rr.IsActive)
                .GroupBy(rr => rr.RoleId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(rr => rr.NavigationRouteId).ToList());

            var response = roles.Select(r => MapToResponse(r,
                routeAssignments.GetValueOrDefault(r.Id, new List<int>()))).ToList();

            return Ok(response);
        }

        /// <summary>
        /// Gets a role by ID, including its assigned route IDs.
        /// System administrators can access roles from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(RoleResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RoleResponse>> GetById(int id)
        {
            var query = _context.Roles.Where(r => r.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var role = await query.FirstOrDefaultAsync();

            if (role is null)
                return NotFound(new { message = "Role not found." });

            var routeIds = await _context.RoleNavigationRoutes
                .Where(rr => rr.RoleId == id && rr.IsActive)
                .Select(rr => rr.NavigationRouteId)
                .ToListAsync();

            return Ok(MapToResponse(role, routeIds));
        }

        /// <summary>
        /// Creates a new role. System administrators may pass a CompanyId to create
        /// the role directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(RoleResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<RoleResponse>> Create([FromBody] CreateRoleRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Only the system administrator can create roles in another company." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Invalid company." });

            var nameExists = await _context.Roles
                .AnyAsync(r => r.Name == request.Name && r.CompanyId == companyId && r.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A role with this name already exists." });

            var role = new Role
            {
                Name = request.Name,
                Description = request.Description,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role created: {Id} - {Name} by {CreatedBy}", role.Id, role.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = role.Id }, MapToResponse(role, new List<int>()));
        }

        /// <summary>
        /// Updates an existing role. System administrators can update roles from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(RoleResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RoleResponse>> Update(int id, [FromBody] UpdateRoleRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Roles.Where(r => r.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var role = await query.FirstOrDefaultAsync();

            if (role is null)
                return NotFound(new { message = "Role not found." });

            var nameExists = await _context.Roles
                .AnyAsync(r => r.Name == request.Name && r.CompanyId == role.CompanyId && r.Id != id && r.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A role with this name already exists." });

            role.Name = request.Name;
            role.Description = request.Description;
            role.ModifiedAt = DateTime.UtcNow;
            role.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            var routeIds = await _context.RoleNavigationRoutes
                .Where(rr => rr.RoleId == id && rr.IsActive)
                .Select(rr => rr.NavigationRouteId)
                .ToListAsync();

            _logger.LogInformation("Role updated: {Id} - {Name} by {ModifiedBy}", role.Id, role.Name, currentUser);

            return Ok(MapToResponse(role, routeIds));
        }

        /// <summary>
        /// Deactivates a role (soft delete). System administrators can deactivate roles
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Roles.Where(r => r.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var role = await query.FirstOrDefaultAsync();

            if (role is null)
                return NotFound(new { message = "Role not found." });

            // Check if any active users are using this role
            var usersWithRole = await _context.Users
                .AnyAsync(u => u.RoleId == id && u.IsActive);
            if (usersWithRole)
                return BadRequest(new { message = "Cannot deactivate role. There are active users assigned to it." });

            role.IsActive = false;
            role.ModifiedAt = DateTime.UtcNow;
            role.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Role deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Role deactivated successfully." });
        }

        /// <summary>
        /// Gets the navigation routes assigned to a role. System administrators can access
        /// roles from any company.
        /// </summary>
        [HttpGet("{id:int}/routes")]
        [ProducesResponseType(typeof(List<NavigationRouteResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<NavigationRouteResponse>>> GetRoutes(int id)
        {
            var roleQuery = _context.Roles.Where(r => r.Id == id);
            if (!IsSystemAdmin())
                roleQuery = roleQuery.Where(r => r.CompanyId == GetCompanyId());

            var roleExists = await roleQuery.AnyAsync();
            if (!roleExists)
                return NotFound(new { message = "Role not found." });

            var routes = await _context.RoleNavigationRoutes
                .Where(rr => rr.RoleId == id && rr.IsActive)
                .Include(rr => rr.NavigationRoute)
                .Select(rr => new NavigationRouteResponse
                {
                    Id = rr.NavigationRoute.Id,
                    ParentId = rr.NavigationRoute.ParentId,
                    WindowName = rr.NavigationRoute.WindowName,
                    RoutePath = rr.NavigationRoute.RoutePath,
                    Icon = rr.NavigationRoute.Icon,
                    WindowId = rr.NavigationRoute.WindowId,
                    Level = rr.NavigationRoute.Level,
                    SortOrder = rr.NavigationRoute.SortOrder,
                    CompanyId = rr.NavigationRoute.CompanyId,
                    IsActive = rr.NavigationRoute.IsActive
                })
                .OrderBy(r => r.Level)
                .ThenBy(r => r.SortOrder)
                .ToListAsync();

            return Ok(routes);
        }

        /// <summary>
        /// Assigns navigation routes to a role. Replaces all current assignments.
        /// System administrators can manage roles from any company.
        /// </summary>
        [HttpPost("{id:int}/routes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AssignRoutes(int id, [FromBody] AssignRoutesRequest request)
        {
            var currentUser = GetCurrentUsername();

            var roleQuery = _context.Roles.Where(r => r.Id == id);
            if (!IsSystemAdmin())
                roleQuery = roleQuery.Where(r => r.CompanyId == GetCompanyId());

            var role = await roleQuery.FirstOrDefaultAsync();
            if (role is null)
                return NotFound(new { message = "Role not found." });

            // Validate all route IDs belong to the same company as the role
            var validRouteIds = await _context.NavigationRoutes
                .Where(r => request.RouteIds.Contains(r.Id) && r.CompanyId == role.CompanyId)
                .Select(r => r.Id)
                .ToListAsync();

            // Get ALL existing assignments (active and inactive)
            var existingAssignments = await _context.RoleNavigationRoutes
                .Where(rr => rr.RoleId == id)
                .ToListAsync();

            // Deactivate assignments not in the new list
            foreach (var assignment in existingAssignments)
            {
                assignment.IsActive = validRouteIds.Contains(assignment.NavigationRouteId);
            }

            // Add only truly new assignments (not previously existing)
            var existingRouteIds = existingAssignments.Select(a => a.NavigationRouteId).ToHashSet();
            foreach (var routeId in validRouteIds.Where(rid => !existingRouteIds.Contains(rid)))
            {
                _context.RoleNavigationRoutes.Add(new RoleNavigationRoute
                {
                    RoleId = id,
                    NavigationRouteId = routeId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Routes assigned to role {RoleId}: {Count} routes by {User}",
                id, validRouteIds.Count, currentUser);

            return Ok(new { message = $"{validRouteIds.Count} routes assigned successfully.", routeIds = validRouteIds });
        }

        private static RoleResponse MapToResponse(Role role, List<int> routeIds)
        {
            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CompanyId = role.CompanyId,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy,
                ModifiedAt = role.ModifiedAt,
                ModifiedBy = role.ModifiedBy,
                RouteIds = routeIds
            };
        }
    }
}
