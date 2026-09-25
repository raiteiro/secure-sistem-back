using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Common;
using SecureSistem.DTOs.Sales;
using SecureSistem.Models;
using SecureSistem.Services;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// The point-of-sale checkout itself. Creating a sale snapshots product prices/tax
    /// rates, validates payments cover the total exactly, and atomically deducts stock
    /// from the chosen warehouse via InventoryMovement — all within one transaction.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ISaleService _saleService;
        private readonly IEmailService _emailService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(
            ApplicationDbContext context, ISaleService saleService, IEmailService emailService, ILogger<SalesController> logger)
        {
            _context = context;
            _saleService = saleService;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Gets sales for the authenticated user's company, newest first, optionally
        /// filtered and paginated. System administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<SaleResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<SaleResponse>>> GetAll(
            [FromQuery] int? branchId, [FromQuery] int? customerId,
            [FromQuery] int? cashSessionId, [FromQuery] string? status,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = BaseQuery();

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            if (branchId is not null)
                query = query.Where(s => s.BranchId == branchId);

            if (customerId is not null)
                query = query.Where(s => s.CustomerId == customerId);

            if (cashSessionId is not null)
                query = query.Where(s => s.CashSessionId == cashSessionId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            if (from is not null)
                query = query.Where(s => s.CreatedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(s => s.CreatedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var sales = await query
                .OrderByDescending(s => s.CreatedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .ToListAsync();

            return Ok(sales.Select(MapToResponse).ToList().ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets a sale by ID. System administrators can access sales from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SaleResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SaleResponse>> GetById(int id)
        {
            var query = BaseQuery().Where(s => s.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var sale = await query.FirstOrDefaultAsync();

            if (sale is null)
                return NotFound(new { message = "Venta no encontrada." });

            return Ok(MapToResponse(sale));
        }

        /// <summary>
        /// Rings up a sale: validates stock, snapshots prices/taxes, records payments
        /// (must sum exactly to the total), and deducts inventory — all in one transaction,
        /// via ISaleService (shared with QuotesController's convert-to-sale). With
        /// CashSessionId, it's a till sale (must be the caller's own open session); without
        /// it, it's a direct sale (e.g. invoiced, paid by transfer) — still requires the
        /// same exact-payment-total, just no till to reconcile against.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SaleResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<SaleResponse>> Create([FromBody] CreateSaleRequest request)
        {
            var userId = GetUserId();
            var currentUser = GetCurrentUsername();
            int companyId;

            if (request.CashSessionId is not null)
            {
                var session = await _context.CashSessions
                    .FirstOrDefaultAsync(s => s.Id == request.CashSessionId);
                if (session is null || session.ClosedAt is not null)
                    return BadRequest(new { message = "El turno de caja debe estar abierto." });

                if (session.UserId != userId)
                    return StatusCode(403, new { message = "Solo puedes registrar ventas contra tu propio turno de caja abierto." });

                companyId = session.CompanyId;
                if (!IsSystemAdmin() && companyId != GetCompanyId())
                    return StatusCode(403, new { message = "Solo puedes registrar ventas para tu propia empresa." });
            }
            else
            {
                companyId = GetCompanyId();
                if (request.CompanyId is not null && request.CompanyId != companyId)
                {
                    if (!IsSystemAdmin())
                        return StatusCode(403, new { message = "Solo el administrador del sistema puede registrar ventas directas en otra empresa." });

                    companyId = request.CompanyId.Value;
                }
            }

            var (saleId, error) = await _saleService.CreateSaleAsync(new SaleCreationRequest
            {
                CompanyId = companyId,
                BranchId = request.BranchId,
                WarehouseId = request.WarehouseId,
                CustomerId = request.CustomerId,
                CashSessionId = request.CashSessionId,
                UserId = userId,
                CurrentUser = currentUser,
                Items = request.Items.Select(i => new SaleLineItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    DiscountAmount = i.DiscountAmount
                }).ToList(),
                Payments = request.Payments
            });

            if (error is not null)
                return BadRequest(new { message = error });

            var created = await BaseQuery().FirstAsync(s => s.Id == saleId);

            _logger.LogInformation("Sale created: {Id} (folio {Folio}) total {Total} by {CreatedBy}",
                created.Id, created.FolioNumber, created.Total, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(created));
        }

        /// <summary>
        /// Cancels a completed sale in full: reverses its stock back into the original
        /// warehouse and marks it "Cancelled". Only allowed while the sale's own cash
        /// session is still open (same-shift correction) and only if nothing has been
        /// returned from it yet — for anything else, use POST /api/returns instead.
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(SaleResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SaleResponse>> Cancel(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Sales
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Include(s => s.CashSession)
                .Where(s => s.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var sale = await query.FirstOrDefaultAsync();

            if (sale is null)
                return NotFound(new { message = "Venta no encontrada." });

            if (sale.Status != "Completed")
                return BadRequest(new { message = $"Esta venta ya está '{sale.Status}'." });

            if (sale.CashSession is not null && sale.CashSession.ClosedAt is not null)
                return BadRequest(new { message = "No se puede cancelar una venta cuyo turno de caja ya está cerrado. Usa una devolución en su lugar." });

            var hasReturns = await _context.Returns.AnyAsync(r => r.SaleId == sale.Id);
            if (hasReturns)
                return BadRequest(new { message = "No se puede cancelar una venta que ya tiene devoluciones. Usa una devolución para lo que quede pendiente." });

            var saleItemIds = sale.Items.Select(i => i.Id).ToList();
            var hasSettledConsignment = await _context.ConsignmentSales
                .AnyAsync(cs => saleItemIds.Contains(cs.SaleItemId) && cs.SettlementId != null);
            if (hasSettledConsignment)
                return BadRequest(new { message = "No se puede cancelar: ya se liquidó al consignador por uno o más artículos de esta venta." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;

            var consignmentSales = await _context.ConsignmentSales
                .Where(cs => saleItemIds.Contains(cs.SaleItemId) && !cs.IsVoided)
                .ToListAsync();
            foreach (var consignmentSale in consignmentSales)
                consignmentSale.IsVoided = true;

            foreach (var item in sale.Items)
            {
                if (item.Product.IsCombo)
                {
                    var comboItems = await _context.ProductComboItems
                        .Where(ci => ci.ComboProductId == item.ProductId && ci.IsActive)
                        .ToListAsync();

                    foreach (var comboItem in comboItems)
                    {
                        await InventoryStockHelper.RestoreStockAsync(
                            _context, comboItem.ComponentProductId, sale.WarehouseId, sale.CompanyId,
                            comboItem.Quantity * item.Quantity, "Return",
                            $"Sale folio {sale.FolioNumber} cancelled (combo '{item.Product.Name}')", currentUser, now);
                    }
                }
                else
                {
                    await InventoryStockHelper.RestoreStockAsync(
                        _context, item.ProductId, sale.WarehouseId, sale.CompanyId,
                        item.Quantity, "Return", $"Sale folio {sale.FolioNumber} cancelled", currentUser, now);
                }
            }

            sale.Status = "Cancelled";
            sale.ModifiedAt = now;
            sale.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var updated = await BaseQuery().FirstAsync(s => s.Id == sale.Id);

            _logger.LogInformation("Sale cancelled: {Id} (folio {Folio}) by {ModifiedBy}",
                sale.Id, sale.FolioNumber, currentUser);

            return Ok(MapToResponse(updated));
        }

        /// <summary>
        /// Gets a print-ready receipt for a sale: the full sale detail plus the company
        /// header (name, tax id, address, phone, logo) needed for a ticket that SaleResponse
        /// doesn't carry.
        /// </summary>
        [HttpGet("{id:int}/receipt")]
        [ProducesResponseType(typeof(ReceiptResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ReceiptResponse>> GetReceipt(int id)
        {
            var query = BaseQuery().Include(s => s.Company).Where(s => s.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var sale = await query.FirstOrDefaultAsync();

            if (sale is null)
                return NotFound(new { message = "Venta no encontrada." });

            return Ok(MapToReceipt(sale));
        }

        /// <summary>
        /// Emails the receipt to the sale's customer, or to an explicit address if one is
        /// given (required when the sale has no customer, or the customer has no email).
        /// </summary>
        [HttpPost("{id:int}/send-receipt")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(502)]
        public async Task<IActionResult> SendReceipt(int id, [FromBody] SendReceiptRequest request)
        {
            var query = BaseQuery().Include(s => s.Company).Where(s => s.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var sale = await query.FirstOrDefaultAsync();

            if (sale is null)
                return NotFound(new { message = "Venta no encontrada." });

            var email = request.Email ?? sale.Customer?.Email;
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "No hay un correo disponible: proporciona uno, o vincula un cliente con correo registrado." });

            var receipt = MapToReceipt(sale);
            var body = BuildReceiptEmailBody(receipt);

            try
            {
                await _emailService.SendAsync(email, $"Recibo de tu compra - Folio {sale.FolioNumber}", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send receipt email for sale {Id}", sale.Id);
                return StatusCode(502, new { message = "No se pudo enviar el recibo por correo. Intenta de nuevo más tarde." });
            }

            _logger.LogInformation("Receipt sent: sale {Id} (folio {Folio}) to {Email}", sale.Id, sale.FolioNumber, email);

            return Ok(new { message = $"Recibo enviado a {email}." });
        }

        private static ReceiptResponse MapToReceipt(Sale sale)
        {
            var response = MapToResponse(sale);
            return new ReceiptResponse
            {
                SaleId = sale.Id,
                FolioNumber = sale.FolioNumber,
                CreatedAt = sale.CreatedAt,
                Status = sale.Status,
                CompanyName = sale.Company.Name,
                CompanyTaxId = sale.Company.TaxId,
                CompanyAddress = sale.Company.Address,
                CompanyPhone = sale.Company.Phone,
                CompanyLogoPath = sale.Company.LogoPath,
                BranchName = sale.Branch.Name,
                CashierUsername = sale.User.Username,
                CustomerName = sale.Customer?.Name,
                CustomerEmail = sale.Customer?.Email,
                Items = response.Items,
                Payments = response.Payments,
                Subtotal = sale.Subtotal,
                DiscountTotal = sale.DiscountTotal,
                TaxTotal = sale.TaxTotal,
                Total = sale.Total
            };
        }

        private static string BuildReceiptEmailBody(ReceiptResponse receipt)
        {
            var sb = new StringBuilder();
            sb.AppendLine(receipt.CompanyName);
            if (!string.IsNullOrWhiteSpace(receipt.CompanyTaxId)) sb.AppendLine(receipt.CompanyTaxId);
            if (!string.IsNullOrWhiteSpace(receipt.CompanyAddress)) sb.AppendLine(receipt.CompanyAddress);
            if (!string.IsNullOrWhiteSpace(receipt.CompanyPhone)) sb.AppendLine(receipt.CompanyPhone);
            sb.AppendLine();
            sb.AppendLine($"Folio: {receipt.FolioNumber}");
            sb.AppendLine($"Fecha: {receipt.CreatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Sucursal: {receipt.BranchName}");
            sb.AppendLine($"Atendió: {receipt.CashierUsername}");
            if (!string.IsNullOrWhiteSpace(receipt.CustomerName)) sb.AppendLine($"Cliente: {receipt.CustomerName}");
            sb.AppendLine();

            foreach (var item in receipt.Items)
            {
                sb.AppendLine($"{item.Quantity} x {item.ProductName} @ {item.UnitPrice:0.00} = {item.Total:0.00}");
            }

            sb.AppendLine();
            sb.AppendLine($"Subtotal: {receipt.Subtotal:0.00}");
            if (receipt.DiscountTotal > 0) sb.AppendLine($"Descuento: -{receipt.DiscountTotal:0.00}");
            sb.AppendLine($"Impuestos: {receipt.TaxTotal:0.00}");
            sb.AppendLine($"Total: {receipt.Total:0.00}");
            sb.AppendLine();

            foreach (var payment in receipt.Payments)
            {
                sb.AppendLine($"Pago ({payment.Method}): {payment.Amount:0.00}");
            }

            sb.AppendLine();
            sb.AppendLine("¡Gracias por tu compra!");

            return sb.ToString();
        }

        private IQueryable<Sale> BaseQuery()
        {
            return _context.Sales
                .Include(s => s.Branch)
                .Include(s => s.Warehouse)
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Include(s => s.Payments);
        }

        private static SaleResponse MapToResponse(Sale sale)
        {
            return new SaleResponse
            {
                Id = sale.Id,
                FolioNumber = sale.FolioNumber,
                BranchId = sale.BranchId,
                BranchName = sale.Branch.Name,
                WarehouseId = sale.WarehouseId,
                WarehouseName = sale.Warehouse.Name,
                CustomerId = sale.CustomerId,
                CustomerName = sale.Customer?.Name,
                CashSessionId = sale.CashSessionId,
                QuoteId = sale.QuoteId,
                UserId = sale.UserId,
                Username = sale.User.Username,
                Status = sale.Status,
                Subtotal = sale.Subtotal,
                DiscountTotal = sale.DiscountTotal,
                TaxTotal = sale.TaxTotal,
                Total = sale.Total,
                Items = sale.Items.Select(i => new SaleItemResponse
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
                Payments = sale.Payments.Select(p => new PaymentResponse
                {
                    Id = p.Id,
                    Method = p.Method,
                    Amount = p.Amount,
                    CreatedAt = p.CreatedAt
                }).ToList(),
                CompanyId = sale.CompanyId,
                CreatedAt = sale.CreatedAt,
                CreatedBy = sale.CreatedBy,
                ModifiedAt = sale.ModifiedAt,
                ModifiedBy = sale.ModifiedBy
            };
        }
    }
}
