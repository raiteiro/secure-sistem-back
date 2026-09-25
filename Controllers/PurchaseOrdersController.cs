using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.PurchaseOrders;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Orders placed with a supplier. Creating one never touches inventory — only receiving
    /// it (fully or partially) does, via the same InventoryStockHelper used everywhere else,
    /// tagged with the order's Supplier for traceability.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrdersController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(ApplicationDbContext context, ILogger<PurchaseOrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets purchase orders for the authenticated user's company, newest first,
        /// optionally filtered and paginated. System administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<PurchaseOrderResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<PurchaseOrderResponse>>> GetAll(
            [FromQuery] int? supplierId, [FromQuery] string? status,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = BaseQuery();

            if (!IsSystemAdmin())
                query = query.Where(po => po.CompanyId == GetCompanyId());

            if (supplierId is not null)
                query = query.Where(po => po.SupplierId == supplierId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(po => po.Status == status);

            if (from is not null)
                query = query.Where(po => po.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(po => po.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(po => po.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .ToListAsync();

            return Ok(orders.Select(MapToResponse).ToList().ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets a purchase order by ID. System administrators can access orders from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PurchaseOrderResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PurchaseOrderResponse>> GetById(int id)
        {
            var query = BaseQuery().Where(po => po.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(po => po.CompanyId == GetCompanyId());

            var order = await query.FirstOrDefaultAsync();

            if (order is null)
                return NotFound(new { message = "Orden de compra no encontrada." });

            return Ok(MapToResponse(order));
        }

        /// <summary>
        /// Creates a purchase order. Doesn't touch inventory — that only happens on receipt.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PurchaseOrderResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<PurchaseOrderResponse>> Create([FromBody] CreatePurchaseOrderRequest request)
        {
            var userId = GetUserId();
            var currentUser = GetCurrentUsername();

            var companyId = GetCompanyId();
            if (request.CompanyId is not null && request.CompanyId != companyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Solo el administrador del sistema puede crear órdenes de compra en otra empresa." });

                companyId = request.CompanyId.Value;
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == request.SupplierId && s.CompanyId == companyId && s.IsActive);
            if (supplier is null)
                return BadRequest(new { message = "El proveedor debe pertenecer a la misma empresa." });

            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.CompanyId == companyId && w.IsActive);
            if (warehouse is null)
                return BadRequest(new { message = "El almacén debe pertenecer a la misma empresa." });

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var validProductCount = await _context.Products
                .CountAsync(p => productIds.Contains(p.Id) && p.CompanyId == companyId && p.IsActive);
            if (validProductCount != productIds.Count)
                return BadRequest(new { message = "Uno o más productos no son válidos." });

            var now = DateTimeHelper.Now;

            var items = request.Items.Select(i => new PurchaseOrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                Total = i.Quantity * i.UnitCost,
                QuantityReceived = 0
            }).ToList();

            var folioNumber = 1 + await _context.PurchaseOrders
                .Where(po => po.CompanyId == companyId)
                .Select(po => (int?)po.FolioNumber)
                .MaxAsync() ?? 1;

            var order = new PurchaseOrder
            {
                FolioNumber = folioNumber,
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                UserId = userId,
                CompanyId = companyId,
                Status = "Pending",
                Notes = request.Notes,
                Total = items.Sum(i => i.Total),
                CreatedAt = now,
                CreatedBy = currentUser,
                Items = items
            };

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(po => po.Id == order.Id);

            _logger.LogInformation("Purchase order created: {Id} (folio {Folio}) total {Total} by {CreatedBy}",
                order.Id, order.FolioNumber, order.Total, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToResponse(created));
        }

        /// <summary>
        /// Receives all or part of a pending purchase order: adds stock to the order's
        /// warehouse for each item received (tagged with the order's Supplier), and marks
        /// the order "Received" once every line has been received in full. Can be called
        /// more than once for the same order if a delivery arrives in parts.
        /// </summary>
        [HttpPost("{id:int}/receive")]
        [ProducesResponseType(typeof(PurchaseOrderResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PurchaseOrderResponse>> Receive(int id, [FromBody] ReceivePurchaseOrderRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.PurchaseOrders.Include(po => po.Items).ThenInclude(i => i.Product).Where(po => po.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(po => po.CompanyId == GetCompanyId());

            var order = await query.FirstOrDefaultAsync();

            if (order is null)
                return NotFound(new { message = "Orden de compra no encontrada." });

            if (order.Status != "Pending")
                return BadRequest(new { message = $"Esta orden ya está '{order.Status}'." });

            var itemIds = request.Items.Select(i => i.PurchaseOrderItemId).ToList();
            if (itemIds.Distinct().Count() != itemIds.Count)
                return BadRequest(new { message = "Hay artículos duplicados en la solicitud." });

            var itemsById = order.Items.ToDictionary(i => i.Id);

            foreach (var itemRequest in request.Items)
            {
                if (!itemsById.TryGetValue(itemRequest.PurchaseOrderItemId, out var item))
                    return BadRequest(new { message = "Uno o más artículos no pertenecen a esta orden." });

                var pending = item.Quantity - item.QuantityReceived;
                if (itemRequest.Quantity > pending)
                    return BadRequest(new
                    {
                        message = $"No se pueden recibir {itemRequest.Quantity} de '{item.Product.Name}'. Cantidad pendiente: {pending}."
                    });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;

            foreach (var itemRequest in request.Items)
            {
                var item = itemsById[itemRequest.PurchaseOrderItemId];

                await InventoryStockHelper.RestoreStockAsync(
                    _context, item.ProductId, order.WarehouseId, order.CompanyId, itemRequest.Quantity,
                    "Purchase", $"Purchase order folio {order.FolioNumber}", currentUser, now, order.SupplierId);

                item.QuantityReceived += itemRequest.Quantity;
            }

            var fullyReceived = order.Items.All(i => i.QuantityReceived >= i.Quantity);
            if (fullyReceived)
            {
                order.Status = "Received";
                order.ReceivedAt = now;
                order.ReceivedBy = currentUser;
            }

            if (!string.IsNullOrWhiteSpace(request.Notes))
                order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? request.Notes : $"{order.Notes}\n{request.Notes}";

            order.ModifiedAt = now;
            order.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var updated = await BaseQuery().FirstAsync(po => po.Id == order.Id);

            _logger.LogInformation("Purchase order received (partial or full): {Id} (folio {Folio}) by {ModifiedBy}, status now {Status}",
                order.Id, order.FolioNumber, currentUser, order.Status);

            return Ok(MapToResponse(updated));
        }

        /// <summary>
        /// Cancels a purchase order. Only allowed while nothing has been received yet.
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(PurchaseOrderResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PurchaseOrderResponse>> Cancel(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.PurchaseOrders.Include(po => po.Items).Where(po => po.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(po => po.CompanyId == GetCompanyId());

            var order = await query.FirstOrDefaultAsync();

            if (order is null)
                return NotFound(new { message = "Orden de compra no encontrada." });

            if (order.Status != "Pending")
                return BadRequest(new { message = $"Esta orden ya está '{order.Status}'." });

            if (order.Items.Any(i => i.QuantityReceived > 0))
                return BadRequest(new { message = "No se puede cancelar: ya se recibió parte de esta orden." });

            order.Status = "Cancelled";
            order.ModifiedAt = DateTimeHelper.Now;
            order.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(po => po.Id == order.Id);

            _logger.LogInformation("Purchase order cancelled: {Id} (folio {Folio}) by {ModifiedBy}", order.Id, order.FolioNumber, currentUser);

            return Ok(MapToResponse(updated));
        }

        private IQueryable<PurchaseOrder> BaseQuery()
        {
            return _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.User)
                .Include(po => po.Items).ThenInclude(i => i.Product);
        }

        private static PurchaseOrderResponse MapToResponse(PurchaseOrder order)
        {
            return new PurchaseOrderResponse
            {
                Id = order.Id,
                FolioNumber = order.FolioNumber,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier.Name,
                WarehouseId = order.WarehouseId,
                WarehouseName = order.Warehouse.Name,
                UserId = order.UserId,
                Username = order.User.Username,
                Status = order.Status,
                Notes = order.Notes,
                Total = order.Total,
                ReceivedAt = order.ReceivedAt,
                ReceivedBy = order.ReceivedBy,
                Items = order.Items.Select(i => new PurchaseOrderItemResponse
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductSku = i.Product.Sku,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    Total = i.Total,
                    QuantityReceived = i.QuantityReceived,
                    QuantityPending = i.Quantity - i.QuantityReceived
                }).ToList(),
                CompanyId = order.CompanyId,
                CreatedAt = order.CreatedAt,
                CreatedBy = order.CreatedBy,
                ModifiedAt = order.ModifiedAt,
                ModifiedBy = order.ModifiedBy
            };
        }
    }
}
