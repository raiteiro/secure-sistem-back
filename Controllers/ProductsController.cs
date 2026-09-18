using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Products;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for the product catalog. Products are shared across all of a
    /// company's branches; per-warehouse stock levels are tracked separately (Inventory).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : BaseApiController
    {
        private static readonly Dictionary<string, string> AllowedImageContentTypes = new()
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };
        private const long MaxImageSizeBytes = 2 * 1024 * 1024; // 2 MB

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active products for the authenticated user's company.
        /// System administrators see products across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ProductResponse>), 200)]
        public async Task<ActionResult<List<ProductResponse>>> GetAll()
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.TaxRate)
                .Where(p => p.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(p => p.CompanyId == GetCompanyId());

            var products = await query
                .OrderBy(p => p.Name)
                .Select(p => MapToResponse(p))
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// Gets a product by ID. System administrators can access products from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProductResponse>> GetById(int id)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.TaxRate)
                .Where(p => p.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(p => p.CompanyId == GetCompanyId());

            var product = await query.FirstOrDefaultAsync();

            if (product is null)
                return NotFound(new { message = "Producto no encontrado." });

            return Ok(MapToResponse(product));
        }

        /// <summary>
        /// Creates a new product. System administrators may pass a CompanyId to create
        /// it directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProductResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Solo el administrador del sistema puede crear productos en otra empresa." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Empresa inválida." });

            var validationError = await ValidateCategoryAndTaxRate(request.CategoryId, request.TaxRateId, companyId);
            if (validationError is not null)
                return BadRequest(new { message = validationError });

            if (request.Sku is not null)
            {
                var skuExists = await _context.Products
                    .AnyAsync(p => p.Sku == request.Sku && p.CompanyId == companyId && p.IsActive);
                if (skuExists)
                    return BadRequest(new { message = "Ya existe un producto con este SKU." });
            }

            var product = new Product
            {
                Sku = request.Sku,
                Name = request.Name,
                Description = request.Description,
                Unit = request.Unit,
                Price = request.Price,
                Cost = request.Cost,
                CategoryId = request.CategoryId,
                TaxRateId = request.TaxRateId,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTimeHelper.Now,
                CreatedBy = currentUser
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            await LoadRelations(product);

            _logger.LogInformation("Product created: {Id} - {Name} by {CreatedBy}", product.Id, product.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, MapToResponse(product));
        }

        /// <summary>
        /// Updates an existing product. System administrators can update products
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(ProductResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProductResponse>> Update(int id, [FromBody] UpdateProductRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Products.Where(p => p.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(p => p.CompanyId == GetCompanyId());

            var product = await query.FirstOrDefaultAsync();

            if (product is null)
                return NotFound(new { message = "Producto no encontrado." });

            var validationError = await ValidateCategoryAndTaxRate(request.CategoryId, request.TaxRateId, product.CompanyId);
            if (validationError is not null)
                return BadRequest(new { message = validationError });

            if (request.Sku is not null)
            {
                var skuExists = await _context.Products
                    .AnyAsync(p => p.Sku == request.Sku && p.CompanyId == product.CompanyId && p.Id != id && p.IsActive);
                if (skuExists)
                    return BadRequest(new { message = "Ya existe un producto con este SKU." });
            }

            product.Sku = request.Sku;
            product.Name = request.Name;
            product.Description = request.Description;
            product.Unit = request.Unit;
            product.Price = request.Price;
            product.Cost = request.Cost;
            product.CategoryId = request.CategoryId;
            product.TaxRateId = request.TaxRateId;
            product.ModifiedAt = DateTimeHelper.Now;
            product.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            await LoadRelations(product);

            _logger.LogInformation("Product updated: {Id} - {Name} by {ModifiedBy}", product.Id, product.Name, currentUser);

            return Ok(MapToResponse(product));
        }

        /// <summary>
        /// Deactivates a product (soft delete). System administrators can deactivate
        /// products from any company.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Products.Where(p => p.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(p => p.CompanyId == GetCompanyId());

            var product = await query.FirstOrDefaultAsync();

            if (product is null)
                return NotFound(new { message = "Producto no encontrado." });

            product.IsActive = false;
            product.ModifiedAt = DateTimeHelper.Now;
            product.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Product deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Producto desactivado correctamente." });
        }

        /// <summary>
        /// Uploads (or replaces) a product's image. System administrators can update any
        /// company's product; everyone else only their own. Accepts PNG, JPEG, WEBP or GIF,
        /// up to 2 MB.
        /// </summary>
        [HttpPost("{id:int}/image")]
        [ProducesResponseType(typeof(ProductResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProductResponse>> UploadImage(int id, IFormFile file)
        {
            var query = _context.Products.Where(p => p.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(p => p.CompanyId == GetCompanyId());

            var product = await query.FirstOrDefaultAsync();
            if (product is null)
                return NotFound(new { message = "Producto no encontrado." });

            var currentUser = GetCurrentUsername();

            if (file is null || file.Length == 0)
                return BadRequest(new { message = "No se subió ningún archivo." });

            if (file.Length > MaxImageSizeBytes)
                return BadRequest(new { message = "La imagen debe pesar 2 MB o menos." });

            if (!AllowedImageContentTypes.TryGetValue(file.ContentType, out var extension))
                return BadRequest(new { message = "La imagen debe ser un archivo PNG, JPEG, WEBP o GIF." });

            var folderRelative = Path.Combine("uploads", "products", id.ToString());
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var folderAbsolute = Path.Combine(webRoot, folderRelative);
            Directory.CreateDirectory(folderAbsolute);

            foreach (var existingFile in Directory.EnumerateFiles(folderAbsolute, "image.*"))
                System.IO.File.Delete(existingFile);

            var fileName = $"image{extension}";
            var fileAbsolutePath = Path.Combine(folderAbsolute, fileName);

            using (var stream = new FileStream(fileAbsolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            product.ImagePath = $"/{folderRelative.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
            product.ModifiedAt = DateTimeHelper.Now;
            product.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            await LoadRelations(product);

            _logger.LogInformation("Image uploaded for product {Id} by {ModifiedBy}", id, currentUser);

            return Ok(MapToResponse(product));
        }

        private async Task<string?> ValidateCategoryAndTaxRate(int? categoryId, int? taxRateId, int companyId)
        {
            if (categoryId is not null)
            {
                var categoryValid = await _context.Categories
                    .AnyAsync(c => c.Id == categoryId && c.CompanyId == companyId && c.IsActive);
                if (!categoryValid)
                    return "La categoría debe pertenecer a la misma empresa.";
            }

            if (taxRateId is not null)
            {
                var taxRateValid = await _context.TaxRates
                    .AnyAsync(t => t.Id == taxRateId && t.CompanyId == companyId && t.IsActive);
                if (!taxRateValid)
                    return "El impuesto debe pertenecer a la misma empresa.";
            }

            return null;
        }

        private async Task LoadRelations(Product product)
        {
            if (product.CategoryId is not null)
                await _context.Entry(product).Reference(p => p.Category).LoadAsync();
            if (product.TaxRateId is not null)
                await _context.Entry(product).Reference(p => p.TaxRate).LoadAsync();
        }

        private static ProductResponse MapToResponse(Product product)
        {
            return new ProductResponse
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Description = product.Description,
                Unit = product.Unit,
                Price = product.Price,
                Cost = product.Cost,
                ImagePath = product.ImagePath,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                TaxRateId = product.TaxRateId,
                TaxRateName = product.TaxRate?.Name,
                TaxRateValue = product.TaxRate?.Rate,
                CompanyId = product.CompanyId,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                CreatedBy = product.CreatedBy,
                ModifiedAt = product.ModifiedAt,
                ModifiedBy = product.ModifiedBy
            };
        }
    }
}
