using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.Inventory;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Records stock movements (in/out/adjustment/purchase/sale/return). Each movement is an immutable ledger
    /// entry — there is no update or delete — and, as a side effect, atomically updates
    /// (or creates) the corresponding Inventory row so it always reflects the ledger.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryMovementsController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryMovementsController> _logger;

        public InventoryMovementsController(ApplicationDbContext context, ILogger<InventoryMovementsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets movements for the authenticated user's company, newest first, optionally
        /// filtered by product or warehouse. System administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<InventoryMovementResponse>), 200)]
        public async Task<ActionResult<List<InventoryMovementResponse>>> GetAll(
            [FromQuery] int? productId, [FromQuery] int? warehouseId)
        {
            var query = _context.InventoryMovements
                .Include(m => m.Product)
                .Include(m => m.Warehouse)
                .AsQueryable();

            if (!IsSystemAdmin())
                query = query.Where(m => m.CompanyId == GetCompanyId());

            if (productId is not null)
                query = query.Where(m => m.ProductId == productId);

            if (warehouseId is not null)
                query = query.Where(m => m.WarehouseId == warehouseId);

            var movements = await query
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => MapToResponse(m))
                .ToListAsync();

            return Ok(movements);
        }

        /// <summary>
        /// Gets a single movement by ID. System administrators can access movements
        /// from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(InventoryMovementResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<InventoryMovementResponse>> GetById(int id)
        {
            var query = _context.InventoryMovements
                .Include(m => m.Product)
                .Include(m => m.Warehouse)
                .Where(m => m.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(m => m.CompanyId == GetCompanyId());

            var movement = await query.FirstOrDefaultAsync();

            if (movement is null)
                return NotFound(new { message = "Movement not found." });

            return Ok(MapToResponse(movement));
        }

        /// <summary>
        /// Records a stock movement and applies it to the corresponding Inventory row.
        /// The target company is derived from the product (and cross-checked against the
        /// warehouse); system administrators may record movements for any company's
        /// products, everyone else only their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(InventoryMovementResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<InventoryMovementResponse>> Create([FromBody] CreateInventoryMovementRequest request)
        {
            var currentUser = GetCurrentUsername();

            if (request.Quantity == 0)
                return BadRequest(new { message = "Quantity cannot be zero." });

            if ((request.Type == "In" || request.Type == "Purchase" || request.Type == "Return") && request.Quantity < 0)
                return BadRequest(new { message = $"'{request.Type}' movements require a positive quantity." });

            if ((request.Type == "Out" || request.Type == "Sale") && request.Quantity > 0)
                return BadRequest(new { message = $"'{request.Type}' movements require a negative quantity." });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);
            if (product is null)
                return BadRequest(new { message = "Invalid product." });

            if (!IsSystemAdmin() && product.CompanyId != GetCompanyId())
                return StatusCode(403, new { message = "Only the system administrator can record movements for another company." });

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == product.CompanyId && w.IsActive);
            if (warehouse is null)
                return BadRequest(new { message = "Warehouse must belong to the same company as the product." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTime.UtcNow;

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId);

            var currentQuantity = inventory?.Quantity ?? 0m;
            var resultingQuantity = currentQuantity + request.Quantity;

            if (resultingQuantity < 0)
                return BadRequest(new { message = $"Insufficient stock. Current: {currentQuantity}, requested change: {request.Quantity}." });

            if (inventory is null)
            {
                inventory = new Inventory
                {
                    ProductId = request.ProductId,
                    WarehouseId = request.WarehouseId,
                    CompanyId = product.CompanyId,
                    Quantity = resultingQuantity,
                    CreatedAt = now,
                    CreatedBy = currentUser
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                inventory.Quantity = resultingQuantity;
                inventory.ModifiedAt = now;
                inventory.ModifiedBy = currentUser;
            }

            var movement = new InventoryMovement
            {
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                CompanyId = product.CompanyId,
                Type = request.Type,
                Quantity = request.Quantity,
                ResultingQuantity = resultingQuantity,
                Notes = request.Notes,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.InventoryMovements.Add(movement);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _context.Entry(movement).Reference(m => m.Product).LoadAsync();
            await _context.Entry(movement).Reference(m => m.Warehouse).LoadAsync();

            _logger.LogInformation(
                "Inventory movement recorded: {Type} {Quantity} of product {ProductId} at warehouse {WarehouseId} by {CreatedBy} (resulting stock: {Resulting})",
                movement.Type, movement.Quantity, movement.ProductId, movement.WarehouseId, currentUser, resultingQuantity);

            return CreatedAtAction(nameof(GetById), new { id = movement.Id }, MapToResponse(movement));
        }

        private static InventoryMovementResponse MapToResponse(InventoryMovement movement)
        {
            return new InventoryMovementResponse
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                ProductName = movement.Product.Name,
                WarehouseId = movement.WarehouseId,
                WarehouseName = movement.Warehouse.Name,
                Type = movement.Type,
                Quantity = movement.Quantity,
                ResultingQuantity = movement.ResultingQuantity,
                Notes = movement.Notes,
                CompanyId = movement.CompanyId,
                CreatedAt = movement.CreatedAt,
                CreatedBy = movement.CreatedBy
            };
        }
    }
}
