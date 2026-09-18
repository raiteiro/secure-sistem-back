using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Reports;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Read-only reporting over sales/inventory/cash-session data already recorded by the
    /// rest of the POS module: sales by day/period, best-selling products, and cashier
    /// close-out history. Nothing here writes data.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : BaseApiController
    {
        private const int DefaultTopProductsLimit = 10;
        private const int MaxTopProductsLimit = 100;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Totals sales (excluding cancelled) grouped by day or month within a date range,
        /// with every product sold nested under its period (same shape as top-products,
        /// not capped to a top N). Defaults to the current month to date when no range is
        /// given.
        /// </summary>
        [HttpGet("sales-by-period")]
        [ProducesResponseType(typeof(SalesByPeriodResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<SalesByPeriodResponse>> GetSalesByPeriod(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? branchId, [FromQuery] string groupBy = "day")
        {
            if (groupBy != "day" && groupBy != "month")
                return BadRequest(new { message = "El parámetro 'groupBy' debe ser 'day' o 'month'." });

            var (rangeStart, rangeEndExclusive) = ResolveDateRange(from, to);
            if (rangeStart > rangeEndExclusive)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var salesQuery = _context.Sales.Where(s =>
                s.Status != "Cancelled" && s.CreatedAt >= rangeStart && s.CreatedAt < rangeEndExclusive);
            var itemsQuery = _context.SaleItems.Where(i =>
                i.Sale.Status != "Cancelled" && i.Sale.CreatedAt >= rangeStart && i.Sale.CreatedAt < rangeEndExclusive);

            if (!IsSystemAdmin())
            {
                salesQuery = salesQuery.Where(s => s.CompanyId == GetCompanyId());
                itemsQuery = itemsQuery.Where(i => i.Sale.CompanyId == GetCompanyId());
            }

            if (branchId is not null)
            {
                salesQuery = salesQuery.Where(s => s.BranchId == branchId);
                itemsQuery = itemsQuery.Where(i => i.Sale.BranchId == branchId);
            }

            List<SalesByPeriodItem> periods;
            Dictionary<DateTime, List<TopProductResponse>> productsByPeriod;

            if (groupBy == "month")
            {
                var grouped = await salesQuery
                    .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Total = g.Sum(s => s.Total) })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToListAsync();

                periods = grouped.Select(g => new SalesByPeriodItem
                {
                    Period = new DateTime(g.Year, g.Month, 1),
                    SalesCount = g.Count,
                    TotalAmount = g.Total
                }).ToList();

                var productRows = await itemsQuery
                    .GroupBy(i => new { i.Sale.CreatedAt.Year, i.Sale.CreatedAt.Month, i.ProductId, i.Product.Name, i.Product.Sku })
                    .Select(g => new
                    {
                        g.Key.Year, g.Key.Month, g.Key.ProductId, g.Key.Name, g.Key.Sku,
                        QuantitySold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Total),
                        UnitPrice = g.Sum(i => i.UnitPrice * i.Quantity) / g.Sum(i => i.Quantity),
                        TaxAmount = g.Sum(i => i.TaxAmount)
                    })
                    .ToListAsync();

                productsByPeriod = productRows
                    .GroupBy(r => new DateTime(r.Year, r.Month, 1))
                    .ToDictionary(g => g.Key, g => g
                        .Select(r => new TopProductResponse
                        {
                            ProductId = r.ProductId,
                            ProductName = r.Name,
                            Sku = r.Sku,
                            QuantitySold = r.QuantitySold,
                            Revenue = r.Revenue,
                            UnitPrice = r.UnitPrice,
                            TaxAmount = r.TaxAmount
                        })
                        .OrderByDescending(p => p.QuantitySold)
                        .ToList());
            }
            else
            {
                var grouped = await salesQuery
                    .GroupBy(s => s.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count(), Total = g.Sum(s => s.Total) })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                periods = grouped.Select(g => new SalesByPeriodItem
                {
                    Period = g.Date,
                    SalesCount = g.Count,
                    TotalAmount = g.Total
                }).ToList();

                var productRows = await itemsQuery
                    .GroupBy(i => new { i.Sale.CreatedAt.Date, i.ProductId, i.Product.Name, i.Product.Sku })
                    .Select(g => new
                    {
                        g.Key.Date, g.Key.ProductId, g.Key.Name, g.Key.Sku,
                        QuantitySold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Total),
                        UnitPrice = g.Sum(i => i.UnitPrice * i.Quantity) / g.Sum(i => i.Quantity),
                        TaxAmount = g.Sum(i => i.TaxAmount)
                    })
                    .ToListAsync();

                productsByPeriod = productRows
                    .GroupBy(r => r.Date)
                    .ToDictionary(g => g.Key, g => g
                        .Select(r => new TopProductResponse
                        {
                            ProductId = r.ProductId,
                            ProductName = r.Name,
                            Sku = r.Sku,
                            QuantitySold = r.QuantitySold,
                            Revenue = r.Revenue,
                            UnitPrice = r.UnitPrice,
                            TaxAmount = r.TaxAmount
                        })
                        .OrderByDescending(p => p.QuantitySold)
                        .ToList());
            }

            foreach (var period in periods)
                period.Products = productsByPeriod.GetValueOrDefault(period.Period, new());

            return Ok(new SalesByPeriodResponse
            {
                From = rangeStart,
                To = rangeEndExclusive.AddDays(-1),
                GroupBy = groupBy,
                Periods = periods,
                TotalSalesCount = periods.Sum(p => p.SalesCount),
                TotalAmount = periods.Sum(p => p.TotalAmount)
            });
        }

        /// <summary>
        /// Best-selling products by quantity within a date range (excludes cancelled sales).
        /// Defaults to the current month to date when no range is given.
        /// </summary>
        [HttpGet("top-products")]
        [ProducesResponseType(typeof(List<TopProductResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<TopProductResponse>>> GetTopProducts(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? branchId, [FromQuery] int limit = DefaultTopProductsLimit)
        {
            if (limit < 1 || limit > MaxTopProductsLimit)
                return BadRequest(new { message = $"El parámetro 'limit' debe estar entre 1 y {MaxTopProductsLimit}." });

            var (rangeStart, rangeEndExclusive) = ResolveDateRange(from, to);
            if (rangeStart > rangeEndExclusive)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = _context.SaleItems.Where(i =>
                i.Sale.Status != "Cancelled" && i.Sale.CreatedAt >= rangeStart && i.Sale.CreatedAt < rangeEndExclusive);

            if (!IsSystemAdmin())
                query = query.Where(i => i.Sale.CompanyId == GetCompanyId());

            if (branchId is not null)
                query = query.Where(i => i.Sale.BranchId == branchId);

            var topProducts = await query
                .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.Sku })
                .Select(g => new TopProductResponse
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    Sku = g.Key.Sku,
                    QuantitySold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Total),
                    UnitPrice = g.Sum(i => i.UnitPrice * i.Quantity) / g.Sum(i => i.Quantity),
                    TaxAmount = g.Sum(i => i.TaxAmount)
                })
                .OrderByDescending(p => p.QuantitySold)
                .Take(limit)
                .ToListAsync();

            return Ok(topProducts);
        }

        /// <summary>
        /// History of cash-register close-outs ("cortes de caja"), one row per closed shift,
        /// with the sales breakdown by payment method behind each one. Filterable by cashier
        /// and date range (matched against ClosedAt). Defaults to the current month to date.
        /// </summary>
        [HttpGet("cashier-closeouts")]
        [ProducesResponseType(typeof(List<CashierCloseoutResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<CashierCloseoutResponse>>> GetCashierCloseouts(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? userId, [FromQuery] int? branchId)
        {
            var (rangeStart, rangeEndExclusive) = ResolveDateRange(from, to);
            if (rangeStart > rangeEndExclusive)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = _context.CashSessions
                .Include(s => s.User)
                .Include(s => s.CashRegister).ThenInclude(r => r.Branch)
                .Where(s => s.ClosedAt != null && s.ClosedAt >= rangeStart && s.ClosedAt < rangeEndExclusive);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            if (userId is not null)
                query = query.Where(s => s.UserId == userId);

            if (branchId is not null)
                query = query.Where(s => s.CashRegister.BranchId == branchId);

            var sessions = await query.OrderByDescending(s => s.ClosedAt).ToListAsync();
            var sessionIds = sessions.Select(s => s.Id).ToList();

            var paymentTotals = await _context.Payments
                .Where(p => sessionIds.Contains(p.Sale.CashSessionId) && p.Sale.Status != "Cancelled")
                .GroupBy(p => new { p.Sale.CashSessionId, p.Method })
                .Select(g => new { g.Key.CashSessionId, g.Key.Method, Total = g.Sum(p => p.Amount) })
                .ToListAsync();

            var salesCounts = await _context.Sales
                .Where(s => sessionIds.Contains(s.CashSessionId) && s.Status != "Cancelled")
                .GroupBy(s => s.CashSessionId)
                .Select(g => new { CashSessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CashSessionId, g => g.Count);

            var refundTotals = await _context.Returns
                .Where(r => sessionIds.Contains(r.CashSessionId))
                .GroupBy(r => r.CashSessionId)
                .Select(g => new { CashSessionId = g.Key, Total = g.Sum(r => r.TotalRefunded) })
                .ToDictionaryAsync(g => g.CashSessionId, g => g.Total);

            decimal PaymentTotal(int cashSessionId, string method) =>
                paymentTotals.Where(p => p.CashSessionId == cashSessionId && p.Method == method)
                    .Select(p => p.Total).FirstOrDefault();

            var result = sessions.Select(session => new CashierCloseoutResponse
            {
                CashSessionId = session.Id,
                UserId = session.UserId,
                Username = session.User.Username,
                CashRegisterId = session.CashRegisterId,
                CashRegisterName = session.CashRegister.Name,
                BranchId = session.CashRegister.BranchId,
                BranchName = session.CashRegister.Branch.Name,
                OpenedAt = session.OpenedAt,
                ClosedAt = session.ClosedAt,
                SalesCount = salesCounts.GetValueOrDefault(session.Id),
                OpeningAmount = session.OpeningAmount,
                CashSalesTotal = PaymentTotal(session.Id, "Cash"),
                CardSalesTotal = PaymentTotal(session.Id, "Card"),
                OtherSalesTotal = PaymentTotal(session.Id, "Other"),
                RefundsTotal = refundTotals.GetValueOrDefault(session.Id),
                ExpectedAmount = session.ExpectedAmount,
                ClosingAmount = session.ClosingAmount,
                Difference = session.Difference
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Resolves the effective [start, endExclusive) range for a report: defaults to the
        /// current month to date (in Mexico City time) when either bound is omitted, and
        /// treats "to" as inclusive of that whole calendar day.
        /// </summary>
        private static (DateTime Start, DateTime EndExclusive) ResolveDateRange(DateTime? from, DateTime? to)
        {
            var now = DateTimeHelper.Now;
            var start = (from ?? new DateTime(now.Year, now.Month, 1)).Date;
            var endExclusive = (to ?? now).Date.AddDays(1);
            return (start, endExclusive);
        }
    }
}
