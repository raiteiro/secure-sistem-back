using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.NavigationRoutes;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Manages navigation menu routes for the application.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NavigationRoutesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NavigationRoutesController> _logger;

        public NavigationRoutesController(ApplicationDbContext context, ILogger<NavigationRoutesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all navigation routes for the authenticated user's company as a tree.
        /// System administrators get the tree across every company.
        /// </summary>
        [HttpGet("tree")]
        [ProducesResponseType(typeof(List<NavigationRouteResponse>), 200)]
        public async Task<ActionResult<List<NavigationRouteResponse>>> GetTree()
        {
            var query = _context.NavigationRoutes.Where(r => r.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var routes = await query
                .OrderBy(r => r.SortOrder)
                .ToListAsync();

            var tree = BuildTree(routes, null);
            return Ok(tree);
        }

        /// <summary>
        /// Gets all navigation routes as a flat list (for admin management).
        /// System administrators see routes across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<NavigationRouteResponse>), 200)]
        public async Task<ActionResult<List<NavigationRouteResponse>>> GetAll()
        {
            var query = _context.NavigationRoutes.AsQueryable();

            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var routes = await query
                .OrderBy(r => r.Level)
                .ThenBy(r => r.SortOrder)
                .Select(r => MapToResponse(r))
                .ToListAsync();

            return Ok(routes);
        }

        /// <summary>
        /// Gets the navigation route tree for the authenticated user,
        /// combining routes assigned directly and inherited from their role.
        /// </summary>
        [HttpGet("my-tree")]
        [ProducesResponseType(typeof(List<NavigationRouteResponse>), 200)]
        public async Task<ActionResult<List<NavigationRouteResponse>>> GetMyTree()
        {
            var userId = GetUserId();
            var roleId = GetRoleId();
            var companyId = GetCompanyId();

            // Routes assigned directly to the user
            var userRouteIds = await _context.UserNavigationRoutes
                .Where(ur => ur.UserId == userId && ur.IsActive)
                .Select(ur => ur.NavigationRouteId)
                .ToListAsync();

            // Routes assigned to the role
            var roleRouteIds = await _context.RoleNavigationRoutes
                .Where(rr => rr.RoleId == roleId && rr.IsActive)
                .Select(rr => rr.NavigationRouteId)
                .ToListAsync();

            var allowedRouteIds = userRouteIds
                .Union(roleRouteIds)
                .Distinct()
                .ToHashSet();

            var allowedRoutes = await _context.NavigationRoutes
                .Where(r => r.CompanyId == companyId
                         && r.IsActive
                         && allowedRouteIds.Contains(r.Id))
                .OrderBy(r => r.SortOrder)
                .ToListAsync();

            // Include parent routes so the tree structure is complete
            var parentIds = allowedRoutes
                .Where(r => r.ParentId.HasValue)
                .Select(r => r.ParentId!.Value)
                .Distinct()
                .Except(allowedRoutes.Select(r => r.Id))
                .ToList();

            if (parentIds.Count > 0)
            {
                var parentRoutes = await _context.NavigationRoutes
                    .Where(r => parentIds.Contains(r.Id) && r.IsActive)
                    .ToListAsync();
                allowedRoutes.AddRange(parentRoutes);
            }

            var tree = BuildTree(allowedRoutes, null);
            return Ok(tree);
        }

        /// <summary>
        /// Gets a single navigation route by ID. System administrators can access routes from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(NavigationRouteResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<NavigationRouteResponse>> GetById(int id)
        {
            var query = _context.NavigationRoutes.Where(r => r.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var route = await query.FirstOrDefaultAsync();

            if (route is null)
                return NotFound(new { message = "Ruta no encontrada." });

            return Ok(MapToResponse(route));
        }

        /// <summary>
        /// Creates a new navigation route. System administrators may pass a CompanyId to
        /// create the route directly in another company; anyone else always creates within
        /// their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(NavigationRouteResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<NavigationRouteResponse>> Create([FromBody] CreateNavigationRouteRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Solo el administrador del sistema puede crear rutas en otra empresa." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Empresa inválida." });

            if (request.ParentId is not null)
            {
                var parentInSameCompany = await _context.NavigationRoutes
                    .AnyAsync(r => r.Id == request.ParentId && r.CompanyId == companyId);
                if (!parentInSameCompany)
                    return BadRequest(new { message = "La ruta padre debe pertenecer a la empresa destino." });
            }

            var route = new NavigationRoute
            {
                ParentId = request.ParentId,
                WindowName = request.WindowName,
                RoutePath = request.RoutePath,
                Icon = request.Icon,
                WindowId = request.WindowId,
                Level = request.Level,
                SortOrder = request.SortOrder,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTimeHelper.Now,
                CreatedBy = currentUser
            };

            _context.NavigationRoutes.Add(route);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Navigation route created: {Id} - {WindowName} by {CreatedBy}",
                route.Id, route.WindowName, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = route.Id }, MapToResponse(route));
        }

        /// <summary>
        /// Updates an existing navigation route. System administrators can update routes
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(NavigationRouteResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<NavigationRouteResponse>> Update(int id, [FromBody] UpdateNavigationRouteRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.NavigationRoutes.Where(r => r.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var route = await query.FirstOrDefaultAsync();

            if (route is null)
                return NotFound(new { message = "Ruta no encontrada." });

            if (request.ParentId is not null)
            {
                var parentInSameCompany = await _context.NavigationRoutes
                    .AnyAsync(r => r.Id == request.ParentId && r.CompanyId == route.CompanyId);
                if (!parentInSameCompany)
                    return BadRequest(new { message = "La ruta padre debe pertenecer a la misma empresa." });
            }

            route.ParentId = request.ParentId;
            route.WindowName = request.WindowName;
            route.RoutePath = request.RoutePath;
            route.Icon = request.Icon;
            route.WindowId = request.WindowId;
            route.Level = request.Level;
            route.SortOrder = request.SortOrder;
            route.IsActive = request.IsActive;
            route.ModifiedAt = DateTimeHelper.Now;
            route.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Navigation route updated: {Id} - {WindowName} by {ModifiedBy}",
                route.Id, route.WindowName, currentUser);

            return Ok(MapToResponse(route));
        }

        /// <summary>
        /// Deactivates a navigation route (soft delete). System administrators can
        /// deactivate routes from any company.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.NavigationRoutes.Where(r => r.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var route = await query.FirstOrDefaultAsync();

            if (route is null)
                return NotFound(new { message = "Ruta no encontrada." });

            route.IsActive = false;
            route.ModifiedAt = DateTimeHelper.Now;
            route.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Navigation route deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Ruta de navegación desactivada correctamente." });
        }

        private static NavigationRouteResponse MapToResponse(NavigationRoute route)
        {
            return new NavigationRouteResponse
            {
                Id = route.Id,
                ParentId = route.ParentId,
                WindowName = route.WindowName,
                RoutePath = route.RoutePath,
                Icon = route.Icon,
                WindowId = route.WindowId,
                Level = route.Level,
                SortOrder = route.SortOrder,
                CompanyId = route.CompanyId,
                IsActive = route.IsActive,
                CreatedAt = route.CreatedAt,
                CreatedBy = route.CreatedBy,
                ModifiedAt = route.ModifiedAt,
                ModifiedBy = route.ModifiedBy
            };
        }

        private static List<NavigationRouteResponse> BuildTree(List<NavigationRoute> allRoutes, int? parentId)
        {
            return allRoutes
                .Where(r => r.ParentId == parentId)
                .Select(r => new NavigationRouteResponse
                {
                    Id = r.Id,
                    ParentId = r.ParentId,
                    WindowName = r.WindowName,
                    RoutePath = r.RoutePath,
                    Icon = r.Icon,
                    WindowId = r.WindowId,
                    Level = r.Level,
                    SortOrder = r.SortOrder,
                    CompanyId = r.CompanyId,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy,
                    ModifiedAt = r.ModifiedAt,
                    ModifiedBy = r.ModifiedBy,
                    Children = BuildTree(allRoutes, r.Id)
                })
                .ToList();
        }
    }
}
