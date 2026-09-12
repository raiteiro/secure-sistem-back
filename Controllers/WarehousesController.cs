using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.Warehouses;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for warehouses (stock locations). A warehouse may optionally belong
    /// to a branch, or stand independent as a central warehouse.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WarehousesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WarehousesController> _logger;

        public WarehousesController(ApplicationDbContext context, ILogger<WarehousesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active warehouses for the authenticated user's company.
        /// System administrators see warehouses across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<WarehouseResponse>), 200)]
        public async Task<ActionResult<List<WarehouseResponse>>> GetAll()
        {
            var query = _context.Warehouses.Include(w => w.Branch).Where(w => w.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(w => w.CompanyId == GetCompanyId());

            var warehouses = await query
                .OrderBy(w => w.Name)
                .Select(w => MapToResponse(w))
                .ToListAsync();

            return Ok(warehouses);
        }

        /// <summary>
        /// Gets a warehouse by ID. System administrators can access warehouses from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(WarehouseResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<WarehouseResponse>> GetById(int id)
        {
            var query = _context.Warehouses.Include(w => w.Branch).Where(w => w.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(w => w.CompanyId == GetCompanyId());

            var warehouse = await query.FirstOrDefaultAsync();

            if (warehouse is null)
                return NotFound(new { message = "Warehouse not found." });

            return Ok(MapToResponse(warehouse));
        }

        /// <summary>
        /// Creates a new warehouse. System administrators may pass a CompanyId to create
        /// the warehouse directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WarehouseResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<WarehouseResponse>> Create([FromBody] CreateWarehouseRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Only the system administrator can create warehouses in another company." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Invalid company." });

            if (request.BranchId is not null)
            {
                var branchInSameCompany = await _context.Branches
                    .AnyAsync(b => b.Id == request.BranchId && b.CompanyId == companyId && b.IsActive);
                if (!branchInSameCompany)
                    return BadRequest(new { message = "Branch must belong to the same target company." });
            }

            var nameExists = await _context.Warehouses
                .AnyAsync(w => w.Name == request.Name && w.CompanyId == companyId && w.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A warehouse with this name already exists." });

            var warehouse = new Warehouse
            {
                Name = request.Name,
                Address = request.Address,
                BranchId = request.BranchId,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();
            await _context.Entry(warehouse).Reference(w => w.Branch).LoadAsync();

            _logger.LogInformation("Warehouse created: {Id} - {Name} by {CreatedBy}", warehouse.Id, warehouse.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, MapToResponse(warehouse));
        }

        /// <summary>
        /// Updates an existing warehouse. System administrators can update warehouses
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(WarehouseResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<WarehouseResponse>> Update(int id, [FromBody] UpdateWarehouseRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Warehouses.Where(w => w.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(w => w.CompanyId == GetCompanyId());

            var warehouse = await query.FirstOrDefaultAsync();

            if (warehouse is null)
                return NotFound(new { message = "Warehouse not found." });

            if (request.BranchId is not null)
            {
                var branchInSameCompany = await _context.Branches
                    .AnyAsync(b => b.Id == request.BranchId && b.CompanyId == warehouse.CompanyId && b.IsActive);
                if (!branchInSameCompany)
                    return BadRequest(new { message = "Branch must belong to the same company." });
            }

            var nameExists = await _context.Warehouses
                .AnyAsync(w => w.Name == request.Name && w.CompanyId == warehouse.CompanyId && w.Id != id && w.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A warehouse with this name already exists." });

            warehouse.Name = request.Name;
            warehouse.Address = request.Address;
            warehouse.BranchId = request.BranchId;
            warehouse.ModifiedAt = DateTime.UtcNow;
            warehouse.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            await _context.Entry(warehouse).Reference(w => w.Branch).LoadAsync();

            _logger.LogInformation("Warehouse updated: {Id} - {Name} by {ModifiedBy}", warehouse.Id, warehouse.Name, currentUser);

            return Ok(MapToResponse(warehouse));
        }

        /// <summary>
        /// Deactivates a warehouse (soft delete). System administrators can deactivate
        /// warehouses from any company. Refuses if it's the only active one left for its
        /// company, since inventory needs at least one place to live.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Warehouses.Where(w => w.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(w => w.CompanyId == GetCompanyId());

            var warehouse = await query.FirstOrDefaultAsync();

            if (warehouse is null)
                return NotFound(new { message = "Warehouse not found." });

            var activeWarehouseCount = await _context.Warehouses
                .CountAsync(w => w.CompanyId == warehouse.CompanyId && w.IsActive);
            if (activeWarehouseCount <= 1)
                return BadRequest(new { message = "Cannot deactivate the only active warehouse of a company." });

            warehouse.IsActive = false;
            warehouse.ModifiedAt = DateTime.UtcNow;
            warehouse.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Warehouse deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Warehouse deactivated successfully." });
        }

        private static WarehouseResponse MapToResponse(Warehouse warehouse)
        {
            return new WarehouseResponse
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Address = warehouse.Address,
                CompanyId = warehouse.CompanyId,
                BranchId = warehouse.BranchId,
                BranchName = warehouse.Branch?.Name,
                IsActive = warehouse.IsActive,
                CreatedAt = warehouse.CreatedAt,
                CreatedBy = warehouse.CreatedBy,
                ModifiedAt = warehouse.ModifiedAt,
                ModifiedBy = warehouse.ModifiedBy
            };
        }
    }
}
