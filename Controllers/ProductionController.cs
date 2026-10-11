using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.Production;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// In-house production of manufactured products at a warehouse. Producing consumes the
    /// recipe's supplies and adds the product; disassembling does the reverse. Each run is
    /// recorded as a ProductionOrder (with a snapshot of the recipe), and every stock change
    /// goes through InventoryStockHelper tagged with that order.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductionController> _logger;

        public ProductionController(ApplicationDbContext context, ILogger<ProductionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets how many units of each active manufactured product can be made at each
        /// active warehouse of its company with the current stock of its supplies — same
        /// rule as combo availability (see ComponentAvailabilityHelper) — plus the product's
        /// own stock there (the most that can be disassembled). Display-only and not
        /// paginated (bounded by products × warehouses). System administrators see every
        /// company's.
        /// </summary>
        [HttpGet("availability")]
        [ProducesResponseType(typeof(List<ProductionAvailabilityResponse>), 200)]
        public async Task<ActionResult<List<ProductionAvailabilityResponse>>> GetAvailability(
            [FromQuery] int? warehouseId, [FromQuery] int? productId)
        {
            var productsQuery = _context.Products.Where(p => p.IsManufactured && p.IsActive);

            if (!IsSystemAdmin())
                productsQuery = productsQuery.Where(p => p.CompanyId == GetCompanyId());

            if (productId is not null)
                productsQuery = productsQuery.Where(p => p.Id == productId);

            var products = await productsQuery
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Sku, p.CompanyId, UnitAbbreviation = p.Unit.Abbreviation, UnitDecimalPlaces = p.Unit.DecimalPlaces })
                .ToListAsync();

            if (products.Count == 0)
                return Ok(new List<ProductionAvailabilityResponse>());

            var productIds = products.Select(p => p.Id).ToList();
            var companyIds = products.Select(p => p.CompanyId).Distinct().ToList();

            var recipeItems = await _context.ProductRecipeItems
                .Include(ri => ri.SupplyProduct).ThenInclude(p => p.Unit)
                .Where(ri => productIds.Contains(ri.ProductId) && ri.IsActive)
                .ToListAsync();

            var warehousesQuery = _context.Warehouses.Where(w => w.IsActive && companyIds.Contains(w.CompanyId));
            if (warehouseId is not null)
                warehousesQuery = warehousesQuery.Where(w => w.Id == warehouseId);

            var warehouses = await warehousesQuery
                .OrderBy(w => w.Name)
                .Select(w => new { w.Id, w.Name, w.CompanyId })
                .ToListAsync();

            // Stock of both the supplies and the manufactured products themselves.
            var stockedProductIds = recipeItems.Select(ri => ri.SupplyProductId).Concat(productIds).Distinct().ToList();
            var warehouseIds = warehouses.Select(w => w.Id).ToList();

            var stockByProductAndWarehouse = await _context.Inventories
                .Where(i => stockedProductIds.Contains(i.ProductId) && warehouseIds.Contains(i.WarehouseId))
                .ToDictionaryAsync(i => (i.ProductId, i.WarehouseId), i => i.Quantity);

            var itemsByProduct = recipeItems.ToLookup(ri => ri.ProductId);
            var result = new List<ProductionAvailabilityResponse>();

            foreach (var product in products)
            {
                foreach (var warehouse in warehouses.Where(w => w.CompanyId == product.CompanyId))
                {
                    var supplies = itemsByProduct[product.Id]
                        .OrderBy(ri => ri.SupplyProduct.Name)
                        .Select(ri =>
                        {
                            var stock = stockByProductAndWarehouse.GetValueOrDefault((ri.SupplyProductId, warehouse.Id), 0m);
                            return new ProductionSupplyAvailabilityResponse
                            {
                                SupplyProductId = ri.SupplyProductId,
                                SupplyProductName = ri.SupplyProduct.Name,
                                SupplyUnit = ri.SupplyProduct.Unit.Abbreviation,
                                SupplyUnitDecimalPlaces = ri.SupplyProduct.Unit.DecimalPlaces,
                                QuantityPerUnit = ri.Quantity,
                                Stock = stock,
                                UnitsPossible = ComponentAvailabilityHelper.UnitsPossible(stock, ri.Quantity, ri.SupplyProduct.IsActive),
                                IsActive = ri.SupplyProduct.IsActive
                            };
                        })
                        .ToList();

                    var maxProducible = ComponentAvailabilityHelper.MaxUnits(supplies.Select(s => s.UnitsPossible).ToList());

                    foreach (var supply in supplies)
                        supply.IsLimiting = supply.UnitsPossible == maxProducible;

                    result.Add(new ProductionAvailabilityResponse
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductSku = product.Sku,
                        ProductUnitAbbreviation = product.UnitAbbreviation,
                        ProductUnitDecimalPlaces = product.UnitDecimalPlaces,
                        WarehouseId = warehouse.Id,
                        WarehouseName = warehouse.Name,
                        CurrentStock = stockByProductAndWarehouse.GetValueOrDefault((product.Id, warehouse.Id), 0m),
                        MaxProducible = maxProducible,
                        CompanyId = product.CompanyId,
                        Supplies = supplies
                    });
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Gets production/disassembly history, newest first, optionally filtered and
        /// paginated. System administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ProductionOrderResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<ProductionOrderResponse>>> GetAll(
            [FromQuery] int? warehouseId, [FromQuery] int? productId, [FromQuery] string? type,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = BaseQuery();

            if (!IsSystemAdmin())
                query = query.Where(o => o.CompanyId == GetCompanyId());

            if (warehouseId is not null)
                query = query.Where(o => o.WarehouseId == warehouseId);

            if (productId is not null)
                query = query.Where(o => o.ProductId == productId);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(o => o.Type == type);

            if (from is not null)
                query = query.Where(o => o.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(o => o.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .ToListAsync();

            return Ok(orders.Select(MapToResponse).ToList().ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets a production order by ID. System administrators can access orders from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ProductionOrderResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProductionOrderResponse>> GetById(int id)
        {
            var query = BaseQuery().Where(o => o.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(o => o.CompanyId == GetCompanyId());

            var order = await query.FirstOrDefaultAsync();

            if (order is null)
                return NotFound(new { message = "Orden de producción no encontrada." });

            return Ok(MapToResponse(order));
        }

        /// <summary>
        /// Produces a manufactured product at a warehouse: deducts every supply of its
        /// recipe (recipe quantity × quantity) and adds the product, all in one transaction.
        /// If any supply falls short, nothing changes and the 400 also carries the most
        /// that can be produced with the current stock (maxProducible).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProductionOrderResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ProductionOrderResponse>> Produce([FromBody] CreateProductionRequest request)
        {
            var (product, warehouse, recipe, error) = await LoadForProductionAsync(request);
            if (error is not null)
                return BadRequest(new { message = error });

            if (!product!.IsActive)
                return BadRequest(new { message = $"'{product.Name}' está inactivo y no se puede fabricar." });

            var inactiveSupply = recipe.FirstOrDefault(ri => !ri.SupplyProduct.IsActive);
            if (inactiveSupply is not null)
                return BadRequest(new { message = $"El insumo '{inactiveSupply.SupplyProduct.Name}' de la receta ya no está activo." });

            // Check every supply up front, so a shortfall can report the real maximum.
            var supplyIds = recipe.Select(ri => ri.SupplyProductId).ToList();
            var stockBySupply = await _context.Inventories
                .Where(i => supplyIds.Contains(i.ProductId) && i.WarehouseId == warehouse!.Id)
                .ToDictionaryAsync(i => i.ProductId, i => i.Quantity);

            var shortSupply = recipe.FirstOrDefault(ri =>
                stockBySupply.GetValueOrDefault(ri.SupplyProductId, 0m) < ri.Quantity * request.Quantity);
            if (shortSupply is not null)
            {
                var maxProducible = ComponentAvailabilityHelper.MaxUnits(recipe
                    .Select(ri => ComponentAvailabilityHelper.UnitsPossible(
                        stockBySupply.GetValueOrDefault(ri.SupplyProductId, 0m), ri.Quantity, true))
                    .ToList());

                return BadRequest(new
                {
                    message = $"Stock insuficiente de '{shortSupply.SupplyProduct.Name}'. Con el stock actual se pueden fabricar como máximo {FormatQuantity(maxProducible)}.",
                    maxProducible,
                    supplyProductId = shortSupply.SupplyProductId
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;
            var currentUser = GetCurrentUsername();

            var order = await CreateOrderAsync("Production", product, warehouse!, recipe, request, now, currentUser);
            var notes = $"Producción #{order.FolioNumber}: {FormatQuantity(request.Quantity)} × {product.Name}";

            foreach (var item in recipe)
            {
                // Stock was checked above; this re-check only trips on a concurrent change.
                var stockError = await InventoryStockHelper.DeductStockAsync(
                    _context, item.SupplyProductId, item.SupplyProduct.Name, warehouse!.Id, product.CompanyId,
                    item.Quantity * request.Quantity, "Production", notes, currentUser, now,
                    productionOrderId: order.Id);

                if (stockError is not null)
                    return BadRequest(new { message = stockError });
            }

            await InventoryStockHelper.RestoreStockAsync(
                _context, product.Id, warehouse!.Id, product.CompanyId, request.Quantity,
                "Production", notes, currentUser, now, productionOrderId: order.Id);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Production order {Id} (folio {Folio}): produced {Quantity} of product {ProductId} at warehouse {WarehouseId} by {CreatedBy}",
                order.Id, order.FolioNumber, request.Quantity, product.Id, warehouse.Id, currentUser);

            var created = await BaseQuery().FirstAsync(o => o.Id == order.Id);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToResponse(created));
        }

        /// <summary>
        /// Disassembles a manufactured product at a warehouse: deducts the product and
        /// returns every supply of its current recipe (recipe quantity × quantity), all in
        /// one transaction. Supplies are returned even if inactive — it's real stock back
        /// in the warehouse. Requires at least that much of the product in stock there.
        /// </summary>
        [HttpPost("disassemble")]
        [ProducesResponseType(typeof(ProductionOrderResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ProductionOrderResponse>> Disassemble([FromBody] CreateProductionRequest request)
        {
            var (product, warehouse, recipe, error) = await LoadForProductionAsync(request);
            if (error is not null)
                return BadRequest(new { message = error });

            var productStock = await _context.Inventories
                .Where(i => i.ProductId == product!.Id && i.WarehouseId == warehouse!.Id)
                .Select(i => (decimal?)i.Quantity)
                .FirstOrDefaultAsync() ?? 0m;

            if (productStock < request.Quantity)
                return BadRequest(new
                {
                    message = $"No hay suficiente stock de '{product!.Name}' para separar. Disponible: {FormatQuantity(productStock)}, solicitado: {FormatQuantity(request.Quantity)}.",
                    currentStock = productStock
                });

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;
            var currentUser = GetCurrentUsername();

            var order = await CreateOrderAsync("Disassembly", product!, warehouse!, recipe, request, now, currentUser);
            var notes = $"Separación #{order.FolioNumber}: {FormatQuantity(request.Quantity)} × {product!.Name}";

            var stockError = await InventoryStockHelper.DeductStockAsync(
                _context, product.Id, product.Name, warehouse!.Id, product.CompanyId, request.Quantity,
                "Disassembly", notes, currentUser, now, productionOrderId: order.Id);

            if (stockError is not null)
                return BadRequest(new { message = stockError });

            foreach (var item in recipe)
            {
                await InventoryStockHelper.RestoreStockAsync(
                    _context, item.SupplyProductId, warehouse.Id, product.CompanyId,
                    item.Quantity * request.Quantity, "Disassembly", notes, currentUser, now,
                    productionOrderId: order.Id);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Production order {Id} (folio {Folio}): disassembled {Quantity} of product {ProductId} at warehouse {WarehouseId} by {CreatedBy}",
                order.Id, order.FolioNumber, request.Quantity, product.Id, warehouse.Id, currentUser);

            var created = await BaseQuery().FirstAsync(o => o.Id == order.Id);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToResponse(created));
        }

        /// <summary>
        /// Shared checks for producing and disassembling: the product must be a
        /// manufactured product of the caller's company (any company for system
        /// administrators) with an active recipe, and the warehouse must be active and
        /// belong to the product's company.
        /// </summary>
        private async Task<(Product? Product, Warehouse? Warehouse, List<ProductRecipeItem> Recipe, string? Error)> LoadForProductionAsync(
            CreateProductionRequest request)
        {
            var noRecipe = new List<ProductRecipeItem>();

            var productQuery = _context.Products.Include(p => p.Unit).Where(p => p.Id == request.ProductId);
            if (!IsSystemAdmin())
                productQuery = productQuery.Where(p => p.CompanyId == GetCompanyId());

            var product = await productQuery.FirstOrDefaultAsync();
            if (product is null)
                return (null, null, noRecipe, "Producto inválido.");

            if (!product.IsManufactured)
                return (null, null, noRecipe, $"'{product.Name}' no es un producto fabricado.");

            var decimalsError = QuantityHelper.ValidateDecimals(request.Quantity, product.Unit, product.Name);
            if (decimalsError is not null)
                return (null, null, noRecipe, decimalsError);

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == product.CompanyId && w.IsActive);
            if (warehouse is null)
                return (null, null, noRecipe, "El almacén debe estar activo y pertenecer a la misma empresa que el producto.");

            var recipe = await _context.ProductRecipeItems
                .Include(ri => ri.SupplyProduct)
                .Where(ri => ri.ProductId == product.Id && ri.IsActive)
                .OrderBy(ri => ri.SupplyProduct.Name)
                .ToListAsync();

            if (recipe.Count == 0)
                return (null, null, noRecipe, $"'{product.Name}' no tiene una receta activa.");

            return (product, warehouse, recipe, null);
        }

        /// <summary>
        /// Creates and saves the ProductionOrder (with its recipe snapshot) so its Id and
        /// folio exist before the stock movements that reference it. Must run inside the
        /// caller's transaction.
        /// </summary>
        private async Task<ProductionOrder> CreateOrderAsync(
            string type, Product product, Warehouse warehouse, List<ProductRecipeItem> recipe,
            CreateProductionRequest request, DateTime now, string currentUser)
        {
            var folioNumber = 1 + await _context.ProductionOrders
                .Where(o => o.CompanyId == product.CompanyId)
                .Select(o => (int?)o.FolioNumber)
                .MaxAsync() ?? 1;

            var order = new ProductionOrder
            {
                CompanyId = product.CompanyId,
                FolioNumber = folioNumber,
                Type = type,
                ProductId = product.Id,
                WarehouseId = warehouse.Id,
                Quantity = request.Quantity,
                Notes = request.Notes,
                UserId = GetUserId(),
                CreatedAt = now,
                CreatedBy = currentUser,
                Items = recipe.Select(ri => new ProductionOrderItem
                {
                    SupplyProductId = ri.SupplyProductId,
                    QuantityPerUnit = ri.Quantity,
                    TotalQuantity = ri.Quantity * request.Quantity
                }).ToList()
            };

            _context.ProductionOrders.Add(order);
            await _context.SaveChangesAsync();

            return order;
        }

        /// <summary>
        /// Quantities are decimal(18,4); show them without trailing zeros ("10", "2.5").
        /// </summary>
        private static string FormatQuantity(decimal quantity) => quantity.ToString("0.####");

        private IQueryable<ProductionOrder> BaseQuery()
        {
            return _context.ProductionOrders
                .Include(o => o.Product)
                .Include(o => o.Warehouse)
                .Include(o => o.Items).ThenInclude(i => i.SupplyProduct).ThenInclude(p => p.Unit);
        }

        private static ProductionOrderResponse MapToResponse(ProductionOrder order)
        {
            return new ProductionOrderResponse
            {
                Id = order.Id,
                FolioNumber = order.FolioNumber,
                Type = order.Type,
                ProductId = order.ProductId,
                ProductName = order.Product.Name,
                ProductSku = order.Product.Sku,
                WarehouseId = order.WarehouseId,
                WarehouseName = order.Warehouse.Name,
                Quantity = order.Quantity,
                Notes = order.Notes,
                Items = order.Items
                    .OrderBy(i => i.SupplyProduct.Name)
                    .Select(i => new ProductionOrderItemResponse
                    {
                        SupplyProductId = i.SupplyProductId,
                        SupplyProductName = i.SupplyProduct.Name,
                        SupplyUnit = i.SupplyProduct.Unit.Abbreviation,
                        QuantityPerUnit = i.QuantityPerUnit,
                        TotalQuantity = i.TotalQuantity
                    }).ToList(),
                CompanyId = order.CompanyId,
                CreatedAt = order.CreatedAt,
                CreatedBy = order.CreatedBy
            };
        }
    }
}
