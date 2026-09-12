using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.CashRegisters;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for cash registers. Cashiers open/close shifts (CashSession)
    /// against one of these.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CashRegistersController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CashRegistersController> _logger;

        public CashRegistersController(ApplicationDbContext context, ILogger<CashRegistersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active cash registers for the authenticated user's company.
        /// System administrators see cash registers across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CashRegisterResponse>), 200)]
        public async Task<ActionResult<List<CashRegisterResponse>>> GetAll()
        {
            var query = _context.CashRegisters.Include(c => c.Branch).Where(c => c.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var registers = await query.OrderBy(c => c.Name).ToListAsync();
            var responses = new List<CashRegisterResponse>();
            foreach (var register in registers)
                responses.Add(await MapToResponseAsync(register));

            return Ok(responses);
        }

        /// <summary>
        /// Gets a cash register by ID. System administrators can access registers
        /// from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CashRegisterResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CashRegisterResponse>> GetById(int id)
        {
            var query = _context.CashRegisters.Include(c => c.Branch).Where(c => c.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var register = await query.FirstOrDefaultAsync();

            if (register is null)
                return NotFound(new { message = "Cash register not found." });

            return Ok(await MapToResponseAsync(register));
        }

        /// <summary>
        /// Creates a new cash register. System administrators may pass a CompanyId to
        /// create it directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CashRegisterResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<CashRegisterResponse>> Create([FromBody] CreateCashRegisterRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Only the system administrator can create cash registers in another company." });

                companyId = request.CompanyId.Value;
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.CompanyId == companyId && b.IsActive);
            if (branch is null)
                return BadRequest(new { message = "Branch must belong to the target company." });

            var nameExists = await _context.CashRegisters
                .AnyAsync(c => c.Name == request.Name && c.BranchId == request.BranchId && c.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A cash register with this name already exists at this branch." });

            var register = new CashRegister
            {
                Name = request.Name,
                BranchId = request.BranchId,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.CashRegisters.Add(register);
            await _context.SaveChangesAsync();
            register.Branch = branch;

            _logger.LogInformation("Cash register created: {Id} - {Name} by {CreatedBy}", register.Id, register.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = register.Id }, await MapToResponseAsync(register));
        }

        /// <summary>
        /// Updates an existing cash register. System administrators can update registers
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(CashRegisterResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CashRegisterResponse>> Update(int id, [FromBody] UpdateCashRegisterRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.CashRegisters.Where(c => c.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var register = await query.FirstOrDefaultAsync();

            if (register is null)
                return NotFound(new { message = "Cash register not found." });

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.CompanyId == register.CompanyId && b.IsActive);
            if (branch is null)
                return BadRequest(new { message = "Branch must belong to the same company." });

            var nameExists = await _context.CashRegisters
                .AnyAsync(c => c.Name == request.Name && c.BranchId == request.BranchId && c.Id != id && c.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A cash register with this name already exists at this branch." });

            register.Name = request.Name;
            register.BranchId = request.BranchId;
            register.ModifiedAt = DateTime.UtcNow;
            register.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            register.Branch = branch;

            _logger.LogInformation("Cash register updated: {Id} - {Name} by {ModifiedBy}", register.Id, register.Name, currentUser);

            return Ok(await MapToResponseAsync(register));
        }

        /// <summary>
        /// Deactivates a cash register (soft delete). System administrators can deactivate
        /// registers from any company. Refuses if it has an open session.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.CashRegisters.Where(c => c.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var register = await query.FirstOrDefaultAsync();

            if (register is null)
                return NotFound(new { message = "Cash register not found." });

            var hasOpenSession = await _context.CashSessions
                .AnyAsync(s => s.CashRegisterId == id && s.ClosedAt == null);
            if (hasOpenSession)
                return BadRequest(new { message = "Cannot deactivate a cash register with an open session." });

            register.IsActive = false;
            register.ModifiedAt = DateTime.UtcNow;
            register.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cash register deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Cash register deactivated successfully." });
        }

        private async Task<CashRegisterResponse> MapToResponseAsync(CashRegister register)
        {
            var hasOpenSession = await _context.CashSessions
                .AnyAsync(s => s.CashRegisterId == register.Id && s.ClosedAt == null);

            return new CashRegisterResponse
            {
                Id = register.Id,
                Name = register.Name,
                BranchId = register.BranchId,
                BranchName = register.Branch.Name,
                CompanyId = register.CompanyId,
                IsActive = register.IsActive,
                HasOpenSession = hasOpenSession,
                CreatedAt = register.CreatedAt,
                CreatedBy = register.CreatedBy,
                ModifiedAt = register.ModifiedAt,
                ModifiedBy = register.ModifiedBy
            };
        }
    }
}
