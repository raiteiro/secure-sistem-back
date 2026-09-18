using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Categories;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for product categories.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active categories for the authenticated user's company.
        /// System administrators see categories across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryResponse>), 200)]
        public async Task<ActionResult<List<CategoryResponse>>> GetAll()
        {
            var query = _context.Categories.Where(c => c.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var categories = await query
                .OrderBy(c => c.Name)
                .Select(c => MapToResponse(c))
                .ToListAsync();

            return Ok(categories);
        }

        /// <summary>
        /// Gets a category by ID. System administrators can access categories from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CategoryResponse>> GetById(int id)
        {
            var query = _context.Categories.Where(c => c.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var category = await query.FirstOrDefaultAsync();

            if (category is null)
                return NotFound(new { message = "Categoría no encontrada." });

            return Ok(MapToResponse(category));
        }

        /// <summary>
        /// Creates a new category. System administrators may pass a CompanyId to create
        /// it directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CategoryResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<CategoryResponse>> Create([FromBody] CreateCategoryRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Solo el administrador del sistema puede crear categorías en otra empresa." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Empresa inválida." });

            var nameExists = await _context.Categories
                .AnyAsync(c => c.Name == request.Name && c.CompanyId == companyId && c.IsActive);
            if (nameExists)
                return BadRequest(new { message = "Ya existe una categoría con este nombre." });

            var category = new Category
            {
                Name = request.Name,
                Description = request.Description,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTimeHelper.Now,
                CreatedBy = currentUser
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category created: {Id} - {Name} by {CreatedBy}", category.Id, category.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, MapToResponse(category));
        }

        /// <summary>
        /// Updates an existing category. System administrators can update categories
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(CategoryResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CategoryResponse>> Update(int id, [FromBody] UpdateCategoryRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Categories.Where(c => c.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var category = await query.FirstOrDefaultAsync();

            if (category is null)
                return NotFound(new { message = "Categoría no encontrada." });

            var nameExists = await _context.Categories
                .AnyAsync(c => c.Name == request.Name && c.CompanyId == category.CompanyId && c.Id != id && c.IsActive);
            if (nameExists)
                return BadRequest(new { message = "Ya existe una categoría con este nombre." });

            category.Name = request.Name;
            category.Description = request.Description;
            category.ModifiedAt = DateTimeHelper.Now;
            category.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Category updated: {Id} - {Name} by {ModifiedBy}", category.Id, category.Name, currentUser);

            return Ok(MapToResponse(category));
        }

        /// <summary>
        /// Deactivates a category (soft delete). System administrators can deactivate
        /// categories from any company. Refuses if any active product still uses it.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Categories.Where(c => c.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var category = await query.FirstOrDefaultAsync();

            if (category is null)
                return NotFound(new { message = "Categoría no encontrada." });

            var productsWithCategory = await _context.Products
                .AnyAsync(p => p.CategoryId == id && p.IsActive);
            if (productsWithCategory)
                return BadRequest(new { message = "No se puede desactivar la categoría. Hay productos activos asignados a ella." });

            category.IsActive = false;
            category.ModifiedAt = DateTimeHelper.Now;
            category.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Category deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Categoría desactivada correctamente." });
        }

        private static CategoryResponse MapToResponse(Category category)
        {
            return new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                CompanyId = category.CompanyId,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                CreatedBy = category.CreatedBy,
                ModifiedAt = category.ModifiedAt,
                ModifiedBy = category.ModifiedBy
            };
        }
    }
}
