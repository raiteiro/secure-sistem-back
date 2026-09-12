using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.Branches;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for company branches (physical locations). Sales, cash registers
    /// and inventory are scoped to a branch.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BranchesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BranchesController> _logger;

        public BranchesController(ApplicationDbContext context, ILogger<BranchesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active branches for the authenticated user's company.
        /// System administrators see branches across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<BranchResponse>), 200)]
        public async Task<ActionResult<List<BranchResponse>>> GetAll()
        {
            var query = _context.Branches.Where(b => b.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(b => b.CompanyId == GetCompanyId());

            var branches = await query
                .OrderBy(b => b.Name)
                .Select(b => MapToResponse(b))
                .ToListAsync();

            return Ok(branches);
        }

        /// <summary>
        /// Gets a branch by ID. System administrators can access branches from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BranchResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<BranchResponse>> GetById(int id)
        {
            var query = _context.Branches.Where(b => b.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(b => b.CompanyId == GetCompanyId());

            var branch = await query.FirstOrDefaultAsync();

            if (branch is null)
                return NotFound(new { message = "Branch not found." });

            return Ok(MapToResponse(branch));
        }

        /// <summary>
        /// Creates a new branch, along with a default warehouse for it (the common case is
        /// one warehouse per branch; additional/independent warehouses can still be added
        /// separately via <see cref="WarehousesController"/>). System administrators may pass
        /// a CompanyId to create the branch directly in another company; anyone else always
        /// creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(BranchResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<BranchResponse>> Create([FromBody] CreateBranchRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Only the system administrator can create branches in another company." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Invalid company." });

            var nameExists = await _context.Branches
                .AnyAsync(b => b.Name == request.Name && b.CompanyId == companyId && b.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A branch with this name already exists." });

            var warehouseName = $"Almacén {request.Name}";
            var warehouseNameExists = await _context.Warehouses
                .AnyAsync(w => w.Name == warehouseName && w.CompanyId == companyId && w.IsActive);
            if (warehouseNameExists)
                return BadRequest(new { message = "A warehouse with the auto-generated name for this branch already exists." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;

            var branch = new Branch
            {
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            var warehouse = new Warehouse
            {
                Name = warehouseName,
                Address = request.Address,
                BranchId = branch.Id,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Branch created: {Id} - {Name} by {CreatedBy}, with default warehouse '{Warehouse}'",
                branch.Id, branch.Name, currentUser, warehouse.Name);

            return CreatedAtAction(nameof(GetById), new { id = branch.Id }, MapToResponse(branch));
        }

        /// <summary>
        /// Updates an existing branch. System administrators can update branches from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(BranchResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<BranchResponse>> Update(int id, [FromBody] UpdateBranchRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Branches.Where(b => b.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(b => b.CompanyId == GetCompanyId());

            var branch = await query.FirstOrDefaultAsync();

            if (branch is null)
                return NotFound(new { message = "Branch not found." });

            var nameExists = await _context.Branches
                .AnyAsync(b => b.Name == request.Name && b.CompanyId == branch.CompanyId && b.Id != id && b.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A branch with this name already exists." });

            branch.Name = request.Name;
            branch.Address = request.Address;
            branch.Phone = request.Phone;
            branch.ModifiedAt = DateTime.UtcNow;
            branch.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch updated: {Id} - {Name} by {ModifiedBy}", branch.Id, branch.Name, currentUser);

            return Ok(MapToResponse(branch));
        }

        /// <summary>
        /// Deactivates a branch (soft delete). System administrators can deactivate branches
        /// from any company. Refuses if the branch is the only active one left for its company,
        /// since every company needs at least one branch to operate the point of sale.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Branches.Where(b => b.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(b => b.CompanyId == GetCompanyId());

            var branch = await query.FirstOrDefaultAsync();

            if (branch is null)
                return NotFound(new { message = "Branch not found." });

            var activeBranchCount = await _context.Branches
                .CountAsync(b => b.CompanyId == branch.CompanyId && b.IsActive);
            if (activeBranchCount <= 1)
                return BadRequest(new { message = "Cannot deactivate the only active branch of a company." });

            branch.IsActive = false;
            branch.ModifiedAt = DateTime.UtcNow;
            branch.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Branch deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Branch deactivated successfully." });
        }

        private static BranchResponse MapToResponse(Branch branch)
        {
            return new BranchResponse
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                Phone = branch.Phone,
                CompanyId = branch.CompanyId,
                IsActive = branch.IsActive,
                CreatedAt = branch.CreatedAt,
                CreatedBy = branch.CreatedBy,
                ModifiedAt = branch.ModifiedAt,
                ModifiedBy = branch.ModifiedBy
            };
        }
    }
}
