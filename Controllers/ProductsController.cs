using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
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
        [ProducesResponseType(typeof(PagedResponse<ProductResponse>), 200)]
        public async Task<ActionResult<PagedResponse<ProductResponse>>> GetAll(
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.TaxRate)
                .Include(p => p.Supplier)
                .Where(p => p.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(p => p.CompanyId == GetCompanyId());

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Name)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .Select(p => MapToResponse(p))
                .ToListAsync();

            return Ok(products.ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
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
                .Include(p => p.Supplier)
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

            var (supplierValidationError, isConsignor) = await ValidateSupplierAndCommission(
                request.SupplierId, request.CommissionType, request.CommissionValue, companyId);
            if (supplierValidationError is not null)
                return BadRequest(new { message = supplierValidationError });

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
                Cost = ResolveCost(isConsignor, request.CommissionType, request.CommissionValue, request.Price, request.Cost),
                CategoryId = request.CategoryId,
                TaxRateId = request.TaxRateId,
                IsCombo = request.IsCombo,
                SupplierId = request.SupplierId,
                CommissionType = request.SupplierId is null ? null : request.CommissionType,
                CommissionValue = request.SupplierId is null ? null : request.CommissionValue,
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

            var (supplierValidationError, isConsignor) = await ValidateSupplierAndCommission(
                request.SupplierId, request.CommissionType, request.CommissionValue, product.CompanyId);
            if (supplierValidationError is not null)
                return BadRequest(new { message = supplierValidationError });

            if (request.Sku is not null)
            {
                var skuExists = await _context.Products
                    .AnyAsync(p => p.Sku == request.Sku && p.CompanyId == product.CompanyId && p.Id != id && p.IsActive);
                if (skuExists)
                    return BadRequest(new { message = "Ya existe un producto con este SKU." });
            }

            if (product.IsCombo && !request.IsCombo)
            {
                var hasComboItems = await _context.ProductComboItems.AnyAsync(ci => ci.ComboProductId == id && ci.IsActive);
                if (hasComboItems)
                    return BadRequest(new { message = "No se puede quitar la marca de combo mientras tenga componentes asignados. Quita los componentes primero." });
            }

            product.Sku = request.Sku;
            product.Name = request.Name;
            product.Description = request.Description;
            product.Unit = request.Unit;
            product.Price = request.Price;
            product.Cost = ResolveCost(isConsignor, request.CommissionType, request.CommissionValue, request.Price, request.Cost);
            product.CategoryId = request.CategoryId;
            product.TaxRateId = request.TaxRateId;
            product.IsCombo = request.IsCombo;
            product.SupplierId = request.SupplierId;
            product.CommissionType = request.SupplierId is null ? null : request.CommissionType;
            product.CommissionValue = request.SupplierId is null ? null : request.CommissionValue;
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

        /// <summary>
        /// Gets the components (and quantities) that make up a combo product. System
        /// administrators can access products from any company.
        /// </summary>
        [HttpGet("{id:int}/combo-items")]
        [ProducesResponseType(typeof(List<ComboItemResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<ComboItemResponse>>> GetComboItems(int id)
        {
            var productQuery = _context.Products.Where(p => p.Id == id);
            if (!IsSystemAdmin())
                productQuery = productQuery.Where(p => p.CompanyId == GetCompanyId());

            var productExists = await productQuery.AnyAsync();
            if (!productExists)
                return NotFound(new { message = "Producto no encontrado." });

            var items = await _context.ProductComboItems
                .Where(ci => ci.ComboProductId == id && ci.IsActive)
                .Include(ci => ci.ComponentProduct)
                .Select(ci => new ComboItemResponse
                {
                    ComponentProductId = ci.ComponentProductId,
                    ComponentProductName = ci.ComponentProduct.Name,
                    ComponentProductSku = ci.ComponentProduct.Sku,
                    Quantity = ci.Quantity
                })
                .OrderBy(i => i.ComponentProductName)
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Sets the components of a combo product, replacing all current ones. The product
        /// must already be marked IsCombo, and every component must be an active, non-combo
        /// product of the same company (no combos-of-combos). System administrators can
        /// manage products from any company.
        /// </summary>
        [HttpPost("{id:int}/combo-items")]
        [ProducesResponseType(typeof(List<ComboItemResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<ComboItemResponse>>> AssignComboItems(int id, [FromBody] AssignComboItemsRequest request)
        {
            var currentUser = GetCurrentUsername();

            var productQuery = _context.Products.Where(p => p.Id == id);
            if (!IsSystemAdmin())
                productQuery = productQuery.Where(p => p.CompanyId == GetCompanyId());

            var product = await productQuery.FirstOrDefaultAsync();
            if (product is null)
                return NotFound(new { message = "Producto no encontrado." });

            if (!product.IsCombo)
                return BadRequest(new { message = "El producto debe estar marcado como combo antes de asignarle componentes." });

            var componentIds = request.Items.Select(i => i.ComponentProductId).ToList();
            if (componentIds.Distinct().Count() != componentIds.Count)
                return BadRequest(new { message = "Hay productos duplicados en la lista de componentes." });

            if (componentIds.Contains(id))
                return BadRequest(new { message = "Un combo no puede incluirse a sí mismo como componente." });

            var components = await _context.Products
                .Where(p => componentIds.Contains(p.Id) && p.CompanyId == product.CompanyId && p.IsActive)
                .ToListAsync();

            if (components.Count != componentIds.Count)
                return BadRequest(new { message = "Uno o más componentes no son válidos (deben ser productos activos de la misma empresa)." });

            var nestedCombo = components.FirstOrDefault(p => p.IsCombo);
            if (nestedCombo is not null)
                return BadRequest(new { message = $"'{nestedCombo.Name}' es en sí mismo un combo — no se pueden anidar combos." });

            // Get ALL existing assignments (active and inactive) — never hard-deleted.
            var existingItems = await _context.ProductComboItems
                .Where(ci => ci.ComboProductId == id)
                .ToListAsync();

            var requestByComponentId = request.Items.ToDictionary(i => i.ComponentProductId);
            var now = DateTimeHelper.Now;

            foreach (var existing in existingItems)
            {
                if (requestByComponentId.TryGetValue(existing.ComponentProductId, out var itemRequest))
                {
                    existing.IsActive = true;
                    existing.Quantity = itemRequest.Quantity;
                }
                else
                {
                    existing.IsActive = false;
                }
            }

            var existingComponentIds = existingItems.Select(ci => ci.ComponentProductId).ToHashSet();
            foreach (var itemRequest in request.Items.Where(i => !existingComponentIds.Contains(i.ComponentProductId)))
            {
                _context.ProductComboItems.Add(new ProductComboItem
                {
                    ComboProductId = id,
                    ComponentProductId = itemRequest.ComponentProductId,
                    Quantity = itemRequest.Quantity,
                    IsActive = true,
                    CompanyId = product.CompanyId,
                    CreatedAt = now,
                    CreatedBy = currentUser
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Combo items assigned to product {Id}: {Count} components by {User}",
                id, request.Items.Count, currentUser);

            var componentsById = components.ToDictionary(p => p.Id);
            var response = request.Items.Select(i => new ComboItemResponse
            {
                ComponentProductId = i.ComponentProductId,
                ComponentProductName = componentsById[i.ComponentProductId].Name,
                ComponentProductSku = componentsById[i.ComponentProductId].Sku,
                Quantity = i.Quantity
            }).OrderBy(i => i.ComponentProductName).ToList();

            return Ok(response);
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

        private async Task<(string? Error, bool IsConsignor)> ValidateSupplierAndCommission(int? supplierId, string? commissionType, decimal? commissionValue, int companyId)
        {
            if (supplierId is null)
                return (null, false);

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == supplierId && s.CompanyId == companyId && s.IsActive);
            if (supplier is null)
                return ("El proveedor debe pertenecer a la misma empresa.", false);

            if (supplier.IsConsignor)
            {
                if (commissionType is null || commissionValue is null)
                    return ("Un producto de un proveedor consignador debe tener 'CommissionType' y 'CommissionValue'.", true);

                if (commissionType == "Percentage" && commissionValue > 100)
                    return ("'CommissionValue' no puede ser mayor a 100 cuando 'CommissionType' es 'Percentage'.", true);
            }

            return (null, supplier.IsConsignor);
        }

        /// <summary>
        /// For a consignment product, Cost isn't something the store paid upfront — it's
        /// derived from the commission split so sales/margin reports (which read Cost) stay
        /// consistent with what actually gets paid to the consignor, instead of a manually
        /// typed value that could drift from it.
        /// </summary>
        private static decimal? ResolveCost(bool isConsignor, string? commissionType, decimal? commissionValue, decimal price, decimal? requestedCost)
        {
            if (!isConsignor)
                return requestedCost;

            return commissionType == "FixedAmount"
                ? commissionValue
                : price * (1 - (commissionValue ?? 0m) / 100m);
        }

        private async Task LoadRelations(Product product)
        {
            if (product.CategoryId is not null)
                await _context.Entry(product).Reference(p => p.Category).LoadAsync();
            if (product.TaxRateId is not null)
                await _context.Entry(product).Reference(p => p.TaxRate).LoadAsync();
            if (product.SupplierId is not null)
                await _context.Entry(product).Reference(p => p.Supplier).LoadAsync();
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
                IsCombo = product.IsCombo,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier?.Name,
                SupplierIsConsignor = product.Supplier?.IsConsignor ?? false,
                CommissionType = product.CommissionType,
                CommissionValue = product.CommissionValue,
                CreatedAt = product.CreatedAt,
                CreatedBy = product.CreatedBy,
                ModifiedAt = product.ModifiedAt,
                ModifiedBy = product.ModifiedBy
            };
        }
    }
}
