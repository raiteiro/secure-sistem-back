using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.TaxRates;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for reusable tax rates that can be assigned to products.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TaxRatesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaxRatesController> _logger;

        public TaxRatesController(ApplicationDbContext context, ILogger<TaxRatesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active tax rates for the authenticated user's company.
        /// System administrators see tax rates across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<TaxRateResponse>), 200)]
        public async Task<ActionResult<List<TaxRateResponse>>> GetAll()
        {
            var query = _context.TaxRates.Where(t => t.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(t => t.CompanyId == GetCompanyId());

            var taxRates = await query
                .OrderBy(t => t.Name)
                .Select(t => MapToResponse(t))
                .ToListAsync();

            return Ok(taxRates);
        }

        /// <summary>
        /// Gets a tax rate by ID. System administrators can access tax rates from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TaxRateResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TaxRateResponse>> GetById(int id)
        {
            var query = _context.TaxRates.Where(t => t.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(t => t.CompanyId == GetCompanyId());

            var taxRate = await query.FirstOrDefaultAsync();

            if (taxRate is null)
                return NotFound(new { message = "Tax rate not found." });

            return Ok(MapToResponse(taxRate));
        }

        /// <summary>
        /// Creates a new tax rate. System administrators may pass a CompanyId to create
        /// it directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(TaxRateResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<TaxRateResponse>> Create([FromBody] CreateTaxRateRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Only the system administrator can create tax rates in another company." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Invalid company." });

            var nameExists = await _context.TaxRates
                .AnyAsync(t => t.Name == request.Name && t.CompanyId == companyId && t.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A tax rate with this name already exists." });

            var taxRate = new TaxRate
            {
                Name = request.Name,
                Rate = request.Rate,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.TaxRates.Add(taxRate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tax rate created: {Id} - {Name} by {CreatedBy}", taxRate.Id, taxRate.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = taxRate.Id }, MapToResponse(taxRate));
        }

        /// <summary>
        /// Updates an existing tax rate. System administrators can update tax rates
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(TaxRateResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TaxRateResponse>> Update(int id, [FromBody] UpdateTaxRateRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.TaxRates.Where(t => t.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(t => t.CompanyId == GetCompanyId());

            var taxRate = await query.FirstOrDefaultAsync();

            if (taxRate is null)
                return NotFound(new { message = "Tax rate not found." });

            var nameExists = await _context.TaxRates
                .AnyAsync(t => t.Name == request.Name && t.CompanyId == taxRate.CompanyId && t.Id != id && t.IsActive);
            if (nameExists)
                return BadRequest(new { message = "A tax rate with this name already exists." });

            taxRate.Name = request.Name;
            taxRate.Rate = request.Rate;
            taxRate.ModifiedAt = DateTime.UtcNow;
            taxRate.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tax rate updated: {Id} - {Name} by {ModifiedBy}", taxRate.Id, taxRate.Name, currentUser);

            return Ok(MapToResponse(taxRate));
        }

        /// <summary>
        /// Deactivates a tax rate (soft delete). System administrators can deactivate
        /// tax rates from any company.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.TaxRates.Where(t => t.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(t => t.CompanyId == GetCompanyId());

            var taxRate = await query.FirstOrDefaultAsync();

            if (taxRate is null)
                return NotFound(new { message = "Tax rate not found." });

            taxRate.IsActive = false;
            taxRate.ModifiedAt = DateTime.UtcNow;
            taxRate.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tax rate deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Tax rate deactivated successfully." });
        }

        private static TaxRateResponse MapToResponse(TaxRate taxRate)
        {
            return new TaxRateResponse
            {
                Id = taxRate.Id,
                Name = taxRate.Name,
                Rate = taxRate.Rate,
                CompanyId = taxRate.CompanyId,
                IsActive = taxRate.IsActive,
                CreatedAt = taxRate.CreatedAt,
                CreatedBy = taxRate.CreatedBy,
                ModifiedAt = taxRate.ModifiedAt,
                ModifiedBy = taxRate.ModifiedBy
            };
        }
    }
}
