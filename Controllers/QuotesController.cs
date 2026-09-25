using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.Quotes;
using SecureSistem.Models;
using SecureSistem.Services;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Non-binding price proposals for a customer. Creating/editing a quote never touches
    /// inventory — only converting one to a real Sale does (see ConvertToSale, which shares
    /// ISaleService with SalesController.Create).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ISaleService _saleService;
        private readonly ILogger<QuotesController> _logger;

        public QuotesController(ApplicationDbContext context, ISaleService saleService, ILogger<QuotesController> logger)
        {
            _context = context;
            _saleService = saleService;
            _logger = logger;
        }

        /// <summary>
        /// Gets quotes for the authenticated user's company, newest first, optionally
        /// filtered and paginated. System administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<QuoteResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<QuoteResponse>>> GetAll(
            [FromQuery] int? customerId, [FromQuery] string? status,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = BaseQuery();

            if (!IsSystemAdmin())
                query = query.Where(q => q.CompanyId == GetCompanyId());

            if (customerId is not null)
                query = query.Where(q => q.CustomerId == customerId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(q => q.Status == status);

            if (from is not null)
                query = query.Where(q => q.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(q => q.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var quotes = await query
                .OrderByDescending(q => q.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .ToListAsync();

            var quoteIds = quotes.Select(q => q.Id).ToList();
            var convertedSaleIds = await _context.Sales
                .Where(s => s.QuoteId != null && quoteIds.Contains(s.QuoteId.Value))
                .Select(s => new { QuoteId = s.QuoteId!.Value, s.Id })
                .ToDictionaryAsync(s => s.QuoteId, s => s.Id);

            var responses = quotes.Select(q => MapToResponse(q, convertedSaleIds.GetValueOrDefault(q.Id))).ToList();

            return Ok(responses.ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets a quote by ID. System administrators can access quotes from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(QuoteResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<QuoteResponse>> GetById(int id)
        {
            var query = BaseQuery().Where(q => q.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(q => q.CompanyId == GetCompanyId());

            var quote = await query.FirstOrDefaultAsync();

            if (quote is null)
                return NotFound(new { message = "Cotización no encontrada." });

            var convertedSale = await _context.Sales.FirstOrDefaultAsync(s => s.QuoteId == quote.Id);

            return Ok(MapToResponse(quote, convertedSale?.Id));
        }

        /// <summary>
        /// Creates a quote: snapshots prices/taxes from the current catalog, like a sale,
        /// but touches no inventory and needs no cash session.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(QuoteResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<QuoteResponse>> Create([FromBody] CreateQuoteRequest request)
        {
            var userId = GetUserId();
            var currentUser = GetCurrentUsername();

            var companyId = GetCompanyId();
            if (request.CompanyId is not null && request.CompanyId != companyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Solo el administrador del sistema puede crear cotizaciones en otra empresa." });

                companyId = request.CompanyId.Value;
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.CompanyId == companyId && b.IsActive);
            if (branch is null)
                return BadRequest(new { message = "La sucursal debe pertenecer a la misma empresa." });

            if (request.CustomerId is not null)
            {
                var customerValid = await _context.Customers
                    .AnyAsync(c => c.Id == request.CustomerId && c.CompanyId == companyId && c.IsActive);
                if (!customerValid)
                    return BadRequest(new { message = "El cliente debe pertenecer a la misma empresa." });
            }

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Include(p => p.TaxRate)
                .Where(p => productIds.Contains(p.Id) && p.CompanyId == companyId && p.IsActive)
                .ToListAsync();

            if (products.Count != productIds.Count)
                return BadRequest(new { message = "Uno o más productos no son válidos." });

            var productsById = products.ToDictionary(p => p.Id);
            var now = DateTimeHelper.Now;

            var quoteItems = new List<QuoteItem>();
            decimal subtotal = 0, discountTotal = 0, taxTotal = 0;

            foreach (var itemRequest in request.Items)
            {
                var product = productsById[itemRequest.ProductId];
                var lineGross = itemRequest.Quantity * product.Price;

                if (itemRequest.DiscountAmount > lineGross)
                    return BadRequest(new { message = $"El descuento no puede exceder el total de la línea para el producto '{product.Name}'." });

                var lineSubtotal = lineGross - itemRequest.DiscountAmount;
                var taxRateValue = product.TaxRate?.Rate ?? 0m;
                var lineTax = lineSubtotal * taxRateValue;
                var lineTotal = lineSubtotal + lineTax;

                subtotal += lineGross;
                discountTotal += itemRequest.DiscountAmount;
                taxTotal += lineTax;

                quoteItems.Add(new QuoteItem
                {
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = product.Price,
                    DiscountAmount = itemRequest.DiscountAmount,
                    TaxRateValue = taxRateValue,
                    TaxAmount = lineTax,
                    Subtotal = lineSubtotal,
                    Total = lineTotal
                });
            }

            var folioNumber = 1 + await _context.Quotes
                .Where(q => q.CompanyId == companyId)
                .Select(q => (int?)q.FolioNumber)
                .MaxAsync() ?? 1;

            var quote = new Quote
            {
                FolioNumber = folioNumber,
                BranchId = request.BranchId,
                CustomerId = request.CustomerId,
                UserId = userId,
                CompanyId = companyId,
                Status = "Open",
                ExpiresAt = request.ExpiresAt,
                Notes = request.Notes,
                Subtotal = subtotal,
                DiscountTotal = discountTotal,
                TaxTotal = taxTotal,
                Total = subtotal - discountTotal + taxTotal,
                CreatedAt = now,
                CreatedBy = currentUser,
                Items = quoteItems
            };

            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(q => q.Id == quote.Id);

            _logger.LogInformation("Quote created: {Id} (folio {Folio}) total {Total} by {CreatedBy}",
                quote.Id, quote.FolioNumber, quote.Total, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = quote.Id }, MapToResponse(created, null));
        }

        /// <summary>
        /// Cancels an open quote. Has no effect on inventory (a quote never touched it).
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(QuoteResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<QuoteResponse>> Cancel(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Quotes.Where(q => q.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(q => q.CompanyId == GetCompanyId());

            var quote = await query.FirstOrDefaultAsync();

            if (quote is null)
                return NotFound(new { message = "Cotización no encontrada." });

            if (quote.Status != "Open")
                return BadRequest(new { message = $"Esta cotización ya está '{quote.Status}'." });

            quote.Status = "Cancelled";
            quote.ModifiedAt = DateTimeHelper.Now;
            quote.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(q => q.Id == quote.Id);

            _logger.LogInformation("Quote cancelled: {Id} (folio {Folio}) by {ModifiedBy}", quote.Id, quote.FolioNumber, currentUser);

            return Ok(MapToResponse(updated, null));
        }

        /// <summary>
        /// Converts an open quote into a real Sale, via the same ISaleService SalesController
        /// uses — validates current stock and deducts it (a quote never reserves stock), and
        /// locks in the prices that were actually quoted even if the catalog price changed
        /// since (tax is recalculated from the product's current rate). With CashSessionId,
        /// behaves like a till sale; without it, like a direct sale (still needs Payments
        /// summing exactly to the total).
        /// </summary>
        [HttpPost("{id:int}/convert-to-sale")]
        [ProducesResponseType(typeof(QuoteResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<QuoteResponse>> ConvertToSale(int id, [FromBody] ConvertQuoteToSaleRequest request)
        {
            var userId = GetUserId();
            var currentUser = GetCurrentUsername();

            var query = _context.Quotes.Include(q => q.Items).Where(q => q.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(q => q.CompanyId == GetCompanyId());

            var quote = await query.FirstOrDefaultAsync();

            if (quote is null)
                return NotFound(new { message = "Cotización no encontrada." });

            if (quote.Status != "Open")
                return BadRequest(new { message = $"Esta cotización ya está '{quote.Status}' y no se puede convertir." });

            if (quote.ExpiresAt is not null && quote.ExpiresAt < DateTimeHelper.Now)
                return BadRequest(new { message = "Esta cotización ya expiró." });

            var companyId = quote.CompanyId;

            if (request.CashSessionId is not null)
            {
                var session = await _context.CashSessions.FirstOrDefaultAsync(s => s.Id == request.CashSessionId);
                if (session is null || session.ClosedAt is not null)
                    return BadRequest(new { message = "El turno de caja debe estar abierto." });

                if (session.UserId != userId)
                    return StatusCode(403, new { message = "Solo puedes registrar ventas contra tu propio turno de caja abierto." });

                if (session.CompanyId != companyId)
                    return BadRequest(new { message = "El turno de caja debe pertenecer a la misma empresa que la cotización." });
            }

            var (saleId, error) = await _saleService.CreateSaleAsync(new SaleCreationRequest
            {
                CompanyId = companyId,
                BranchId = quote.BranchId,
                WarehouseId = request.WarehouseId,
                CustomerId = quote.CustomerId,
                CashSessionId = request.CashSessionId,
                QuoteId = quote.Id,
                UserId = userId,
                CurrentUser = currentUser,
                Items = quote.Items.Select(qi => new SaleLineItem
                {
                    ProductId = qi.ProductId,
                    Quantity = qi.Quantity,
                    DiscountAmount = qi.DiscountAmount,
                    UnitPriceOverride = qi.UnitPrice
                }).ToList(),
                Payments = request.Payments
            });

            if (error is not null)
                return BadRequest(new { message = error });

            quote.Status = "Converted";
            quote.ModifiedAt = DateTimeHelper.Now;
            quote.ModifiedBy = currentUser;
            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(q => q.Id == quote.Id);

            _logger.LogInformation("Quote converted to sale: quote {QuoteId} (folio {Folio}) -> sale {SaleId} by {CreatedBy}",
                quote.Id, quote.FolioNumber, saleId, currentUser);

            return Ok(MapToResponse(updated, saleId));
        }

        private IQueryable<Quote> BaseQuery()
        {
            return _context.Quotes
                .Include(q => q.Branch)
                .Include(q => q.Customer)
                .Include(q => q.User)
                .Include(q => q.Items).ThenInclude(i => i.Product);
        }

        private static QuoteResponse MapToResponse(Quote quote, int? convertedSaleId)
        {
            return new QuoteResponse
            {
                Id = quote.Id,
                FolioNumber = quote.FolioNumber,
                BranchId = quote.BranchId,
                BranchName = quote.Branch.Name,
                CustomerId = quote.CustomerId,
                CustomerName = quote.Customer?.Name,
                UserId = quote.UserId,
                Username = quote.User.Username,
                Status = quote.Status,
                ExpiresAt = quote.ExpiresAt,
                Notes = quote.Notes,
                ConvertedSaleId = convertedSaleId,
                Subtotal = quote.Subtotal,
                DiscountTotal = quote.DiscountTotal,
                TaxTotal = quote.TaxTotal,
                Total = quote.Total,
                Items = quote.Items.Select(i => new QuoteItemResponse
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductSku = i.Product.Sku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountAmount = i.DiscountAmount,
                    TaxRateValue = i.TaxRateValue,
                    TaxAmount = i.TaxAmount,
                    Subtotal = i.Subtotal,
                    Total = i.Total
                }).ToList(),
                CompanyId = quote.CompanyId,
                CreatedAt = quote.CreatedAt,
                CreatedBy = quote.CreatedBy,
                ModifiedAt = quote.ModifiedAt,
                ModifiedBy = quote.ModifiedBy
            };
        }
    }
}
