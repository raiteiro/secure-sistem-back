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
        /// filtered by product, warehouse, or date range, and paginated. System
        /// administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<InventoryMovementResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<InventoryMovementResponse>>> GetAll(
            [FromQuery] int? productId, [FromQuery] int? warehouseId,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = _context.InventoryMovements
                .Include(m => m.Product)
                .Include(m => m.Warehouse)
                .Include(m => m.Supplier)
                .AsQueryable();

            if (!IsSystemAdmin())
                query = query.Where(m => m.CompanyId == GetCompanyId());

            if (productId is not null)
                query = query.Where(m => m.ProductId == productId);

            if (warehouseId is not null)
                query = query.Where(m => m.WarehouseId == warehouseId);

            if (from is not null)
                query = query.Where(m => m.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(m => m.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var movements = await query
                .OrderByDescending(m => m.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .Select(m => MapToResponse(m))
                .ToListAsync();

            return Ok(movements.ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
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
                .Include(m => m.Supplier)
                .Where(m => m.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(m => m.CompanyId == GetCompanyId());

            var movement = await query.FirstOrDefaultAsync();

            if (movement is null)
                return NotFound(new { message = "Movimiento no encontrado." });

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
                return BadRequest(new { message = "La cantidad no puede ser cero." });

            if ((request.Type == "In" || request.Type == "Purchase" || request.Type == "Return") && request.Quantity < 0)
                return BadRequest(new { message = $"Los movimientos de tipo '{request.Type}' requieren una cantidad positiva." });

            if ((request.Type == "Out" || request.Type == "Sale") && request.Quantity > 0)
                return BadRequest(new { message = $"Los movimientos de tipo '{request.Type}' requieren una cantidad negativa." });

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);
            if (product is null)
                return BadRequest(new { message = "Producto inválido." });

            if (!IsSystemAdmin() && product.CompanyId != GetCompanyId())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede registrar movimientos para otra empresa." });

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == product.CompanyId && w.IsActive);
            if (warehouse is null)
                return BadRequest(new { message = "El almacén debe pertenecer a la misma empresa que el producto." });

            if (request.SupplierId is not null)
            {
                var supplierValid = await _context.Suppliers
                    .AnyAsync(s => s.Id == request.SupplierId && s.CompanyId == product.CompanyId && s.IsActive);
                if (!supplierValid)
                    return BadRequest(new { message = "El proveedor debe pertenecer a la misma empresa que el producto." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId);

            var currentQuantity = inventory?.Quantity ?? 0m;
            var resultingQuantity = currentQuantity + request.Quantity;

            if (resultingQuantity < 0)
                return BadRequest(new { message = $"Stock insuficiente. Actual: {currentQuantity}, cambio solicitado: {request.Quantity}." });

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
                SupplierId = request.SupplierId,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.InventoryMovements.Add(movement);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _context.Entry(movement).Reference(m => m.Product).LoadAsync();
            await _context.Entry(movement).Reference(m => m.Warehouse).LoadAsync();
            if (movement.SupplierId is not null)
                await _context.Entry(movement).Reference(m => m.Supplier).LoadAsync();

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
                SupplierId = movement.SupplierId,
                SupplierName = movement.Supplier?.Name,
                CompanyId = movement.CompanyId,
                CreatedAt = movement.CreatedAt,
                CreatedBy = movement.CreatedBy
            };
        }
    }
}
