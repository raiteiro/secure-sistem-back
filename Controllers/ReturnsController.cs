using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.Returns;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Partial or full returns of items from a completed sale. Processed against the
    /// caller's own open cash session (which may differ from the original sale's session,
    /// since a return can happen days later) — restocks inventory and records the refund
    /// without altering the original Sale.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReturnsController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReturnsController> _logger;

        public ReturnsController(ApplicationDbContext context, ILogger<ReturnsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets returns for the authenticated user's company, newest first, optionally
        /// filtered and paginated. System administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ReturnResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<ReturnResponse>>> GetAll(
            [FromQuery] int? saleId, [FromQuery] int? cashSessionId,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = BaseQuery();

            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            if (saleId is not null)
                query = query.Where(r => r.SaleId == saleId);

            if (cashSessionId is not null)
                query = query.Where(r => r.CashSessionId == cashSessionId);

            if (from is not null)
                query = query.Where(r => r.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(r => r.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var returns = await query
                .OrderByDescending(r => r.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .ToListAsync();

            return Ok(returns.Select(MapToResponse).ToList().ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets a return by ID. System administrators can access returns from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReturnResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ReturnResponse>> GetById(int id)
        {
            var query = BaseQuery().Where(r => r.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(r => r.CompanyId == GetCompanyId());

            var ret = await query.FirstOrDefaultAsync();

            if (ret is null)
                return NotFound(new { message = "Devolución no encontrada." });

            return Ok(MapToResponse(ret));
        }

        /// <summary>
        /// Registers a return of one or more lines (fully or partially) of a completed sale,
        /// restocking inventory into the sale's original warehouse. Refund amounts are
        /// derived proportionally from each SaleItem's snapshotted price/discount/tax, so
        /// they stay correct even if the line had a discount applied.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ReturnResponse>> Create([FromBody] CreateReturnRequest request)
        {
            var userId = GetUserId();
            var currentUser = GetCurrentUsername();

            var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == request.SaleId);
            if (sale is null)
                return BadRequest(new { message = "Venta inválida." });

            if (!IsSystemAdmin() && sale.CompanyId != GetCompanyId())
                return StatusCode(403, new { message = "Solo puedes procesar devoluciones de ventas de tu propia empresa." });

            if (sale.Status != "Completed")
                return BadRequest(new { message = $"No se pueden devolver artículos de una venta con estado '{sale.Status}'." });

            var session = await _context.CashSessions
                .FirstOrDefaultAsync(s => s.Id == request.CashSessionId);
            if (session is null || session.ClosedAt is not null)
                return BadRequest(new { message = "El turno de caja debe estar abierto." });

            if (session.UserId != userId)
                return StatusCode(403, new { message = "Solo puedes procesar devoluciones contra tu propio turno de caja abierto." });

            if (session.CompanyId != sale.CompanyId)
                return BadRequest(new { message = "El turno de caja debe pertenecer a la misma empresa que la venta." });

            var saleItemIds = request.Items.Select(i => i.SaleItemId).ToList();
            if (saleItemIds.Distinct().Count() != saleItemIds.Count)
                return BadRequest(new { message = "Hay artículos duplicados en la solicitud." });

            var saleItems = await _context.SaleItems
                .Include(i => i.Product)
                .Where(i => saleItemIds.Contains(i.Id) && i.SaleId == sale.Id)
                .ToListAsync();

            if (saleItems.Count != saleItemIds.Count)
                return BadRequest(new { message = "Uno o más artículos no pertenecen a esta venta." });

            var saleItemsById = saleItems.ToDictionary(i => i.Id);

            var alreadyReturned = await _context.ReturnItems
                .Where(ri => saleItemIds.Contains(ri.SaleItemId))
                .GroupBy(ri => ri.SaleItemId)
                .Select(g => new { SaleItemId = g.Key, Quantity = g.Sum(ri => ri.Quantity) })
                .ToDictionaryAsync(g => g.SaleItemId, g => g.Quantity);

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;

            var returnItems = new List<ReturnItem>();
            decimal subtotalRefunded = 0, taxRefunded = 0;

            foreach (var itemRequest in request.Items)
            {
                var saleItem = saleItemsById[itemRequest.SaleItemId];
                var alreadyReturnedQuantity = alreadyReturned.GetValueOrDefault(saleItem.Id, 0m);
                var remaining = saleItem.Quantity - alreadyReturnedQuantity;

                if (itemRequest.Quantity > remaining)
                    return BadRequest(new
                    {
                        message = $"No se pueden devolver {itemRequest.Quantity} de '{saleItem.Product.Name}'. Cantidad restante que se puede devolver: {remaining}."
                    });

                var unitSubtotal = saleItem.Subtotal / saleItem.Quantity;
                var unitTax = saleItem.TaxAmount / saleItem.Quantity;
                var lineSubtotal = unitSubtotal * itemRequest.Quantity;
                var lineTax = unitTax * itemRequest.Quantity;
                var lineTotal = lineSubtotal + lineTax;

                subtotalRefunded += lineSubtotal;
                taxRefunded += lineTax;

                returnItems.Add(new ReturnItem
                {
                    SaleItemId = saleItem.Id,
                    ProductId = saleItem.ProductId,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = saleItem.UnitPrice,
                    TaxRateValue = saleItem.TaxRateValue,
                    Subtotal = lineSubtotal,
                    TaxAmount = lineTax,
                    Total = lineTotal
                });

                // Restock into the sale's original warehouse. A combo carries no stock of its
                // own — restock each of its components instead, scaled by the quantity returned.
                if (saleItem.Product.IsCombo)
                {
                    var comboItems = await _context.ProductComboItems
                        .Where(ci => ci.ComboProductId == saleItem.ProductId && ci.IsActive)
                        .ToListAsync();

                    foreach (var comboItem in comboItems)
                    {
                        await InventoryStockHelper.RestoreStockAsync(
                            _context, comboItem.ComponentProductId, sale.WarehouseId, sale.CompanyId,
                            comboItem.Quantity * itemRequest.Quantity, "Return",
                            $"Return of sale folio {sale.FolioNumber} (combo '{saleItem.Product.Name}')", currentUser, now);
                    }
                }
                else
                {
                    await InventoryStockHelper.RestoreStockAsync(
                        _context, saleItem.ProductId, sale.WarehouseId, sale.CompanyId,
                        itemRequest.Quantity, "Return", $"Return of sale folio {sale.FolioNumber}", currentUser, now);
                }

                // Reduce what's owed to the consignor by the returned share. If it was already
                // settled/paid out, leave it alone and just flag it — reversing a payment
                // already made needs a human, not an automatic adjustment.
                var consignmentSale = await _context.ConsignmentSales
                    .FirstOrDefaultAsync(cs => cs.SaleItemId == saleItem.Id && !cs.IsVoided);
                if (consignmentSale is not null)
                {
                    if (consignmentSale.SettlementId is not null)
                    {
                        _logger.LogWarning(
                            "Return processed for sale item {SaleItemId} whose consignment sale {ConsignmentSaleId} was already settled — ledger NOT adjusted, needs manual reconciliation.",
                            saleItem.Id, consignmentSale.Id);
                    }
                    else if (consignmentSale.Quantity > 0)
                    {
                        var reductionFraction = Math.Min(itemRequest.Quantity / consignmentSale.Quantity, 1m);
                        consignmentSale.SaleAmount -= consignmentSale.SaleAmount * reductionFraction;
                        consignmentSale.ConsignorAmount -= consignmentSale.ConsignorAmount * reductionFraction;
                        consignmentSale.StoreAmount -= consignmentSale.StoreAmount * reductionFraction;
                        consignmentSale.Quantity -= itemRequest.Quantity;
                    }
                }
            }

            var ret = new Return
            {
                SaleId = sale.Id,
                WarehouseId = sale.WarehouseId,
                CashSessionId = request.CashSessionId,
                UserId = userId,
                CompanyId = sale.CompanyId,
                RefundMethod = request.RefundMethod,
                Reason = request.Reason,
                SubtotalRefunded = subtotalRefunded,
                TaxRefunded = taxRefunded,
                TotalRefunded = subtotalRefunded + taxRefunded,
                CreatedAt = now,
                CreatedBy = currentUser,
                Items = returnItems
            };

            _context.Returns.Add(ret);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var created = await BaseQuery().FirstAsync(r => r.Id == ret.Id);

            _logger.LogInformation("Return created: {Id} for sale {SaleId} (folio {Folio}) total {Total} by {CreatedBy}",
                ret.Id, sale.Id, sale.FolioNumber, ret.TotalRefunded, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = ret.Id }, MapToResponse(created));
        }

        private IQueryable<Return> BaseQuery()
        {
            return _context.Returns
                .Include(r => r.Sale)
                .Include(r => r.Warehouse)
                .Include(r => r.User)
                .Include(r => r.Items).ThenInclude(i => i.Product);
        }

        private static ReturnResponse MapToResponse(Return ret)
        {
            return new ReturnResponse
            {
                Id = ret.Id,
                SaleId = ret.SaleId,
                SaleFolioNumber = ret.Sale.FolioNumber,
                WarehouseId = ret.WarehouseId,
                WarehouseName = ret.Warehouse.Name,
                CashSessionId = ret.CashSessionId,
                UserId = ret.UserId,
                Username = ret.User.Username,
                RefundMethod = ret.RefundMethod,
                Reason = ret.Reason,
                SubtotalRefunded = ret.SubtotalRefunded,
                TaxRefunded = ret.TaxRefunded,
                TotalRefunded = ret.TotalRefunded,
                Items = ret.Items.Select(i => new ReturnItemResponse
                {
                    Id = i.Id,
                    SaleItemId = i.SaleItemId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductSku = i.Product.Sku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxRateValue = i.TaxRateValue,
                    Subtotal = i.Subtotal,
                    TaxAmount = i.TaxAmount,
                    Total = i.Total
                }).ToList(),
                CompanyId = ret.CompanyId,
                CreatedAt = ret.CreatedAt,
                CreatedBy = ret.CreatedBy
            };
        }
    }
}
