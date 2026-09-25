using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.Consignment;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Tracks what's owed to consignor suppliers (ConsignmentSale, auto-created by
    /// SalesController.Create for products attributed to a consignor — see
    /// Product.SupplierId/CommissionType/CommissionValue) and settles it (ConsignmentSettlement).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConsignmentController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConsignmentController> _logger;

        public ConsignmentController(ApplicationDbContext context, ILogger<ConsignmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets how much is currently owed to each consignor supplier — pending (unsettled,
        /// not voided) ConsignmentSale rows, grouped and summed. Only suppliers with a
        /// nonzero pending balance are returned.
        /// </summary>
        [HttpGet("balances")]
        [ProducesResponseType(typeof(List<ConsignmentBalanceResponse>), 200)]
        public async Task<ActionResult<List<ConsignmentBalanceResponse>>> GetBalances()
        {
            var query = _context.ConsignmentSales
                .Where(cs => cs.SettlementId == null && !cs.IsVoided);

            if (!IsSystemAdmin())
                query = query.Where(cs => cs.CompanyId == GetCompanyId());

            var balances = await query
                .GroupBy(cs => new { cs.SupplierId, cs.Supplier.Name })
                .Select(g => new ConsignmentBalanceResponse
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.Name,
                    PendingSalesCount = g.Count(),
                    PendingAmount = g.Sum(cs => cs.ConsignorAmount)
                })
                .OrderBy(b => b.SupplierName)
                .ToListAsync();

            return Ok(balances);
        }

        /// <summary>
        /// Gets consignment sale ledger entries, optionally filtered by supplier,
        /// pending-only (unsettled, not voided), and/or date range, and paginated.
        /// </summary>
        [HttpGet("sales")]
        [ProducesResponseType(typeof(PagedResponse<ConsignmentSaleResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<ConsignmentSaleResponse>>> GetSales(
            [FromQuery] int? supplierId, [FromQuery] bool? pending,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = BaseSalesQuery();

            if (!IsSystemAdmin())
                query = query.Where(cs => cs.CompanyId == GetCompanyId());

            if (supplierId is not null)
                query = query.Where(cs => cs.SupplierId == supplierId);

            if (pending == true)
                query = query.Where(cs => cs.SettlementId == null && !cs.IsVoided);

            if (from is not null)
                query = query.Where(cs => cs.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(cs => cs.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var sales = await query
                .OrderByDescending(cs => cs.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .ToListAsync();

            return Ok(sales.Select(MapToSaleResponse).ToList().ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Settles a supplier's entire current pending balance in one go: every unsettled,
        /// non-voided ConsignmentSale for that supplier gets stamped with the new
        /// ConsignmentSettlement's Id. Fails if there's nothing pending to settle.
        /// </summary>
        [HttpPost("settlements")]
        [ProducesResponseType(typeof(SettlementResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<SettlementResponse>> CreateSettlement([FromBody] CreateSettlementRequest request)
        {
            var currentUser = GetCurrentUsername();

            var supplierQuery = _context.Suppliers.Where(s => s.Id == request.SupplierId && s.IsConsignor);
            if (!IsSystemAdmin())
                supplierQuery = supplierQuery.Where(s => s.CompanyId == GetCompanyId());

            var supplier = await supplierQuery.FirstOrDefaultAsync();
            if (supplier is null)
                return BadRequest(new { message = "Proveedor inválido o no es consignador." });

            var pendingSales = await _context.ConsignmentSales
                .Where(cs => cs.SupplierId == request.SupplierId && cs.SettlementId == null && !cs.IsVoided)
                .ToListAsync();

            if (pendingSales.Count == 0)
                return BadRequest(new { message = "Este proveedor no tiene ventas pendientes por liquidar." });

            var now = DateTimeHelper.Now;
            var settlement = new ConsignmentSettlement
            {
                SupplierId = supplier.Id,
                CompanyId = supplier.CompanyId,
                TotalAmount = pendingSales.Sum(cs => cs.ConsignorAmount),
                Notes = request.Notes,
                CreatedAt = now,
                CreatedBy = currentUser
            };

            _context.ConsignmentSettlements.Add(settlement);
            await _context.SaveChangesAsync();

            foreach (var sale in pendingSales)
                sale.SettlementId = settlement.Id;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Consignment settlement created: {Id} for supplier {SupplierId} ({SupplierName}), {Count} sales, total {Total} by {CreatedBy}",
                settlement.Id, supplier.Id, supplier.Name, pendingSales.Count, settlement.TotalAmount, currentUser);

            settlement.Supplier = supplier;
            return CreatedAtAction(nameof(GetSettlementById), new { id = settlement.Id }, await MapToSettlementResponse(settlement));
        }

        /// <summary>
        /// Gets settlement history, newest first, optionally filtered by supplier and/or
        /// date range, and paginated.
        /// </summary>
        [HttpGet("settlements")]
        [ProducesResponseType(typeof(PagedResponse<SettlementResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<SettlementResponse>>> GetSettlements(
            [FromQuery] int? supplierId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = _context.ConsignmentSettlements
                .Include(s => s.Supplier)
                .AsQueryable();

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            if (supplierId is not null)
                query = query.Where(s => s.SupplierId == supplierId);

            if (from is not null)
                query = query.Where(s => s.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(s => s.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var settlements = await query
                .OrderByDescending(s => s.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .ToListAsync();

            var responses = new List<SettlementResponse>();
            foreach (var settlement in settlements)
                responses.Add(await MapToSettlementResponse(settlement));

            return Ok(responses.ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets a settlement by ID, with the full list of sales it covers.
        /// </summary>
        [HttpGet("settlements/{id:int}")]
        [ProducesResponseType(typeof(SettlementResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SettlementResponse>> GetSettlementById(int id)
        {
            var query = _context.ConsignmentSettlements.Include(s => s.Supplier).Where(s => s.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var settlement = await query.FirstOrDefaultAsync();
            if (settlement is null)
                return NotFound(new { message = "Liquidación no encontrada." });

            return Ok(await MapToSettlementResponse(settlement));
        }

        private IQueryable<ConsignmentSale> BaseSalesQuery()
        {
            return _context.ConsignmentSales
                .Include(cs => cs.Product)
                .Include(cs => cs.Supplier)
                .Include(cs => cs.SaleItem).ThenInclude(si => si.Sale);
        }

        private static ConsignmentSaleResponse MapToSaleResponse(ConsignmentSale cs)
        {
            return new ConsignmentSaleResponse
            {
                Id = cs.Id,
                SaleItemId = cs.SaleItemId,
                SaleId = cs.SaleItem.SaleId,
                SaleFolioNumber = cs.SaleItem.Sale.FolioNumber,
                ProductId = cs.ProductId,
                ProductName = cs.Product.Name,
                SupplierId = cs.SupplierId,
                SupplierName = cs.Supplier.Name,
                Quantity = cs.Quantity,
                SaleAmount = cs.SaleAmount,
                ConsignorAmount = cs.ConsignorAmount,
                StoreAmount = cs.StoreAmount,
                IsVoided = cs.IsVoided,
                SettlementId = cs.SettlementId,
                CreatedAt = cs.CreatedAt
            };
        }

        private async Task<SettlementResponse> MapToSettlementResponse(ConsignmentSettlement settlement)
        {
            var sales = await BaseSalesQuery()
                .Where(cs => cs.SettlementId == settlement.Id)
                .ToListAsync();

            return new SettlementResponse
            {
                Id = settlement.Id,
                SupplierId = settlement.SupplierId,
                SupplierName = settlement.Supplier.Name,
                TotalAmount = settlement.TotalAmount,
                Notes = settlement.Notes,
                Sales = sales.Select(MapToSaleResponse).ToList(),
                CompanyId = settlement.CompanyId,
                CreatedAt = settlement.CreatedAt,
                CreatedBy = settlement.CreatedBy
            };
        }
    }
}
