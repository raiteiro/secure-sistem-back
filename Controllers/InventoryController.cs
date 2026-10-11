using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.Inventory;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Read access to current stock levels. Quantity only ever changes as a side effect of
    /// recording a movement via <see cref="InventoryMovementsController"/> — there is no
    /// endpoint here to edit it directly, only the low-stock alert threshold.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(ApplicationDbContext context, ILogger<InventoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets current stock levels for the authenticated user's company, optionally
        /// filtered by product or warehouse. System administrators see every company's.
        /// Combos are excluded: they carry no stock of their own — see GetComboAvailability.
        /// Optionally only one product kind ("Product" or "Supply").
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<InventoryResponse>), 200)]
        public async Task<ActionResult<PagedResponse<InventoryResponse>>> GetAll(
            [FromQuery] int? productId, [FromQuery] int? warehouseId, [FromQuery] bool lowStockOnly = false,
            [FromQuery] string? kind = null, [FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        {
            var query = _context.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Unit)
                .Include(i => i.Warehouse)
                .Where(i => !i.Product.IsCombo);

            if (!IsSystemAdmin())
                query = query.Where(i => i.CompanyId == GetCompanyId());

            if (!string.IsNullOrEmpty(kind))
                query = query.Where(i => i.Product.Kind == kind);

            if (productId is not null)
                query = query.Where(i => i.ProductId == productId);

            if (warehouseId is not null)
                query = query.Where(i => i.WarehouseId == warehouseId);

            if (lowStockOnly)
                query = query.Where(i => i.MinStock != null && i.Quantity <= i.MinStock);

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var inventory = await query
                .OrderBy(i => i.Product.Name)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .Select(i => MapToResponse(i))
                .ToListAsync();

            return Ok(inventory.ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets how many units of each active combo can be sold from each active warehouse
        /// of its company, given the current stock of its components:
        /// min over active components of floor(max(stock, 0) / quantity per combo).
        /// Matches what SaleService would accept with the same data — an inactive component,
        /// a missing Inventory row, or a combo with no active components all yield 0.
        /// Display-only and not paginated (bounded by combos × warehouses). Same company
        /// scoping as GetAll: system administrators see every company's.
        /// </summary>
        [HttpGet("combo-availability")]
        [ProducesResponseType(typeof(List<ComboAvailabilityResponse>), 200)]
        public async Task<ActionResult<List<ComboAvailabilityResponse>>> GetComboAvailability(
            [FromQuery] int? warehouseId, [FromQuery] int? productId)
        {
            var combosQuery = _context.Products.Where(p => p.IsCombo && p.IsActive);

            if (!IsSystemAdmin())
                combosQuery = combosQuery.Where(p => p.CompanyId == GetCompanyId());

            if (productId is not null)
                combosQuery = combosQuery.Where(p => p.Id == productId);

            var combos = await combosQuery
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Sku, p.CompanyId })
                .ToListAsync();

            if (combos.Count == 0)
                return Ok(new List<ComboAvailabilityResponse>());

            var comboIds = combos.Select(c => c.Id).ToList();
            var companyIds = combos.Select(c => c.CompanyId).Distinct().ToList();

            var comboItems = await _context.ProductComboItems
                .Include(ci => ci.ComponentProduct)
                .Where(ci => comboIds.Contains(ci.ComboProductId) && ci.IsActive)
                .ToListAsync();

            var warehousesQuery = _context.Warehouses.Where(w => w.IsActive && companyIds.Contains(w.CompanyId));
            if (warehouseId is not null)
                warehousesQuery = warehousesQuery.Where(w => w.Id == warehouseId);

            var warehouses = await warehousesQuery
                .OrderBy(w => w.Name)
                .Select(w => new { w.Id, w.Name, w.CompanyId })
                .ToListAsync();

            var componentIds = comboItems.Select(ci => ci.ComponentProductId).Distinct().ToList();
            var warehouseIds = warehouses.Select(w => w.Id).ToList();

            var stockByProductAndWarehouse = await _context.Inventories
                .Where(i => componentIds.Contains(i.ProductId) && warehouseIds.Contains(i.WarehouseId))
                .ToDictionaryAsync(i => (i.ProductId, i.WarehouseId), i => i.Quantity);

            var itemsByCombo = comboItems.ToLookup(ci => ci.ComboProductId);
            var result = new List<ComboAvailabilityResponse>();

            foreach (var combo in combos)
            {
                foreach (var warehouse in warehouses.Where(w => w.CompanyId == combo.CompanyId))
                {
                    var components = itemsByCombo[combo.Id]
                        .OrderBy(ci => ci.ComponentProduct.Name)
                        .Select(ci =>
                        {
                            var stock = stockByProductAndWarehouse.GetValueOrDefault((ci.ComponentProductId, warehouse.Id), 0m);
                            return new ComboComponentAvailabilityResponse
                            {
                                ComponentProductId = ci.ComponentProductId,
                                ComponentProductName = ci.ComponentProduct.Name,
                                QuantityPerCombo = ci.Quantity,
                                Stock = stock,
                                CombosPossible = ComponentAvailabilityHelper.UnitsPossible(
                                    stock, ci.Quantity, ci.ComponentProduct.IsActive),
                                IsActive = ci.ComponentProduct.IsActive
                            };
                        })
                        .ToList();

                    var available = ComponentAvailabilityHelper.MaxUnits(components.Select(c => c.CombosPossible).ToList());

                    foreach (var component in components)
                        component.IsLimiting = component.CombosPossible == available;

                    result.Add(new ComboAvailabilityResponse
                    {
                        ComboProductId = combo.Id,
                        ComboProductName = combo.Name,
                        ComboProductSku = combo.Sku,
                        WarehouseId = warehouse.Id,
                        WarehouseName = warehouse.Name,
                        AvailableQuantity = available,
                        CompanyId = combo.CompanyId,
                        Components = components
                    });
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Gets a single inventory record by ID. System administrators can access
        /// records from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(InventoryResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<InventoryResponse>> GetById(int id)
        {
            var query = _context.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Unit)
                .Include(i => i.Warehouse)
                .Where(i => i.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(i => i.CompanyId == GetCompanyId());

            var inventory = await query.FirstOrDefaultAsync();

            if (inventory is null)
                return NotFound(new { message = "Registro de inventario no encontrado." });

            return Ok(MapToResponse(inventory));
        }

        /// <summary>
        /// Sets (or clears) the low-stock alert threshold for a product at a warehouse.
        /// System administrators can update records from any company.
        /// </summary>
        [HttpPost("{id:int}/min-stock")]
        [ProducesResponseType(typeof(InventoryResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<InventoryResponse>> SetMinStock(int id, [FromBody] SetMinStockRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Unit)
                .Include(i => i.Warehouse)
                .Where(i => i.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(i => i.CompanyId == GetCompanyId());

            var inventory = await query.FirstOrDefaultAsync();

            if (inventory is null)
                return NotFound(new { message = "Registro de inventario no encontrado." });

            if (request.MinStock is not null)
            {
                var decimalsError = QuantityHelper.ValidateDecimals(request.MinStock.Value, inventory.Product.Unit, inventory.Product.Name);
                if (decimalsError is not null)
                    return BadRequest(new { message = decimalsError });
            }

            inventory.MinStock = request.MinStock;
            inventory.ModifiedAt = DateTimeHelper.Now;
            inventory.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Min stock updated for inventory {Id} by {ModifiedBy}", id, currentUser);

            return Ok(MapToResponse(inventory));
        }

        private static InventoryResponse MapToResponse(Inventory inventory)
        {
            return new InventoryResponse
            {
                Id = inventory.Id,
                ProductId = inventory.ProductId,
                ProductName = inventory.Product.Name,
                ProductSku = inventory.Product.Sku,
                ProductKind = inventory.Product.Kind,
                IsManufactured = inventory.Product.IsManufactured,
                UnitAbbreviation = inventory.Product.Unit.Abbreviation,
                UnitDecimalPlaces = inventory.Product.Unit.DecimalPlaces,
                WarehouseId = inventory.WarehouseId,
                WarehouseName = inventory.Warehouse.Name,
                Quantity = inventory.Quantity,
                MinStock = inventory.MinStock,
                IsLowStock = inventory.MinStock is not null && inventory.Quantity <= inventory.MinStock,
                CompanyId = inventory.CompanyId,
                CreatedAt = inventory.CreatedAt,
                CreatedBy = inventory.CreatedBy,
                ModifiedAt = inventory.ModifiedAt,
                ModifiedBy = inventory.ModifiedBy
            };
        }
    }
}
