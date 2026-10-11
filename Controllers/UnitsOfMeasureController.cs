using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.UnitsOfMeasure;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// The global unit-of-measure catalog, shared by every company. Anyone authenticated can
    /// read it (to pick a product's unit); only system administrators can change it, since a
    /// change affects every company at once.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UnitsOfMeasureController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitsOfMeasureController> _logger;

        public UnitsOfMeasureController(ApplicationDbContext context, ILogger<UnitsOfMeasureController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets the catalog ordered by name, with how many active products (across every
        /// company) use each unit. Only active units unless includeInactive is true. Not
        /// paginated: the catalog is small and bounded.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<UnitOfMeasureResponse>), 200)]
        public async Task<ActionResult<List<UnitOfMeasureResponse>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _context.UnitsOfMeasure.AsQueryable();

            if (!includeInactive)
                query = query.Where(u => u.IsActive);

            var units = await query
                .OrderBy(u => u.Name)
                .Select(u => new UnitOfMeasureResponse
                {
                    Id = u.Id,
                    Name = u.Name,
                    Abbreviation = u.Abbreviation,
                    DecimalPlaces = u.DecimalPlaces,
                    IsActive = u.IsActive,
                    ProductCount = _context.Products.Count(p => p.UnitId == u.Id && p.IsActive),
                    CreatedAt = u.CreatedAt,
                    CreatedBy = u.CreatedBy,
                    ModifiedAt = u.ModifiedAt,
                    ModifiedBy = u.ModifiedBy
                })
                .ToListAsync();

            return Ok(units);
        }

        /// <summary>
        /// Creates a unit. System administrator only.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UnitOfMeasureResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<UnitOfMeasureResponse>> Create([FromBody] UnitOfMeasureRequest request)
        {
            if (!IsSystemAdmin())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede administrar las unidades de medida." });

            var name = request.Name.Trim();
            var abbreviation = request.Abbreviation.Trim();

            var duplicateError = await ValidateUniqueAsync(name, abbreviation, excludeId: null);
            if (duplicateError is not null)
                return BadRequest(new { message = duplicateError });

            var unit = new UnitOfMeasure
            {
                Name = name,
                Abbreviation = abbreviation,
                DecimalPlaces = request.DecimalPlaces,
                IsActive = true,
                CreatedAt = DateTimeHelper.Now,
                CreatedBy = GetCurrentUsername()
            };

            _context.UnitsOfMeasure.Add(unit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Unit of measure created: {Id} - {Name} by {CreatedBy}", unit.Id, unit.Name, unit.CreatedBy);

            return StatusCode(201, await MapToResponseAsync(unit));
        }

        /// <summary>
        /// Updates a unit. System administrator only. DecimalPlaces can always be raised,
        /// but never lowered while any product (active or not) uses the unit — existing
        /// quantities could stop fitting it.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(UnitOfMeasureResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UnitOfMeasureResponse>> Update(int id, [FromBody] UnitOfMeasureRequest request)
        {
            if (!IsSystemAdmin())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede administrar las unidades de medida." });

            var unit = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id);
            if (unit is null)
                return NotFound(new { message = "Unidad de medida no encontrada." });

            var name = request.Name.Trim();
            var abbreviation = request.Abbreviation.Trim();

            var duplicateError = await ValidateUniqueAsync(name, abbreviation, excludeId: id);
            if (duplicateError is not null)
                return BadRequest(new { message = duplicateError });

            if (request.DecimalPlaces < unit.DecimalPlaces)
            {
                var inUse = await _context.Products.AnyAsync(p => p.UnitId == id);
                if (inUse)
                    return BadRequest(new { message = "La unidad la usan productos: se le pueden agregar decimales, pero no quitar." });
            }

            unit.Name = name;
            unit.Abbreviation = abbreviation;
            unit.DecimalPlaces = request.DecimalPlaces;
            unit.ModifiedAt = DateTimeHelper.Now;
            unit.ModifiedBy = GetCurrentUsername();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Unit of measure updated: {Id} - {Name} by {ModifiedBy}", unit.Id, unit.Name, unit.ModifiedBy);

            return Ok(await MapToResponseAsync(unit));
        }

        /// <summary>
        /// Deactivates a unit (soft delete). System administrator only. Allowed even while
        /// in use: products keep it, but it can no longer be picked for a new product or when
        /// changing a product's unit.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(typeof(UnitOfMeasureResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public Task<ActionResult<UnitOfMeasureResponse>> Deactivate(int id) => SetActiveAsync(id, false);

        /// <summary>
        /// Reactivates a unit. System administrator only.
        /// </summary>
        [HttpPost("{id:int}/activate")]
        [ProducesResponseType(typeof(UnitOfMeasureResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public Task<ActionResult<UnitOfMeasureResponse>> Activate(int id) => SetActiveAsync(id, true);

        private async Task<ActionResult<UnitOfMeasureResponse>> SetActiveAsync(int id, bool isActive)
        {
            if (!IsSystemAdmin())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede administrar las unidades de medida." });

            var unit = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id);
            if (unit is null)
                return NotFound(new { message = "Unidad de medida no encontrada." });

            unit.IsActive = isActive;
            unit.ModifiedAt = DateTimeHelper.Now;
            unit.ModifiedBy = GetCurrentUsername();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Unit of measure {Action}: {Id} by {ModifiedBy}",
                isActive ? "activated" : "deactivated", id, unit.ModifiedBy);

            return Ok(await MapToResponseAsync(unit));
        }

        private async Task<string?> ValidateUniqueAsync(string name, string abbreviation, int? excludeId)
        {
            var lowerName = name.ToLower();
            var lowerAbbreviation = abbreviation.ToLower();

            if (await _context.UnitsOfMeasure.AnyAsync(u => u.Id != excludeId && u.Name.ToLower() == lowerName))
                return "Ya existe una unidad de medida con ese nombre.";

            if (await _context.UnitsOfMeasure.AnyAsync(u => u.Id != excludeId && u.Abbreviation.ToLower() == lowerAbbreviation))
                return "Ya existe una unidad de medida con esa abreviatura.";

            return null;
        }

        private async Task<UnitOfMeasureResponse> MapToResponseAsync(UnitOfMeasure unit)
        {
            return new UnitOfMeasureResponse
            {
                Id = unit.Id,
                Name = unit.Name,
                Abbreviation = unit.Abbreviation,
                DecimalPlaces = unit.DecimalPlaces,
                IsActive = unit.IsActive,
                ProductCount = await _context.Products.CountAsync(p => p.UnitId == unit.Id && p.IsActive),
                CreatedAt = unit.CreatedAt,
                CreatedBy = unit.CreatedBy,
                ModifiedAt = unit.ModifiedAt,
                ModifiedBy = unit.ModifiedBy
            };
        }
    }
}
