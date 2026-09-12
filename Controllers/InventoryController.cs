using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
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
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<InventoryResponse>), 200)]
        public async Task<ActionResult<List<InventoryResponse>>> GetAll(
            [FromQuery] int? productId, [FromQuery] int? warehouseId, [FromQuery] bool lowStockOnly = false)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .AsQueryable();

            if (!IsSystemAdmin())
                query = query.Where(i => i.CompanyId == GetCompanyId());

            if (productId is not null)
                query = query.Where(i => i.ProductId == productId);

            if (warehouseId is not null)
                query = query.Where(i => i.WarehouseId == warehouseId);

            if (lowStockOnly)
                query = query.Where(i => i.MinStock != null && i.Quantity <= i.MinStock);

            var inventory = await query
                .OrderBy(i => i.Product.Name)
                .Select(i => MapToResponse(i))
                .ToListAsync();

            return Ok(inventory);
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
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(i => i.CompanyId == GetCompanyId());

            var inventory = await query.FirstOrDefaultAsync();

            if (inventory is null)
                return NotFound(new { message = "Inventory record not found." });

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
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(i => i.CompanyId == GetCompanyId());

            var inventory = await query.FirstOrDefaultAsync();

            if (inventory is null)
                return NotFound(new { message = "Inventory record not found." });

            inventory.MinStock = request.MinStock;
            inventory.ModifiedAt = DateTime.UtcNow;
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
