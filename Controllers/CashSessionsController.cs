using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.CashSessions;
using SecureSistem.DTOs.Common;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Opens and closes cashier shifts against a cash register. While a session is open,
    /// sales can be rung up against it; closing one reconciles the counted cash against
    /// what was expected.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CashSessionsController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CashSessionsController> _logger;

        public CashSessionsController(ApplicationDbContext context, ILogger<CashSessionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets sessions for the authenticated user's company, newest first, optionally
        /// filtered by cash register, open/closed status, or opened-date range, and
        /// paginated. System administrators see every company's.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<CashSessionResponse>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResponse<CashSessionResponse>>> GetAll(
            [FromQuery] int? cashRegisterId, [FromQuery] bool? isOpen,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int? page, [FromQuery] int? pageSize)
        {
            if (from is not null && to is not null && from.Value.Date > to.Value.Date)
                return BadRequest(new { message = "La fecha 'from' no puede ser posterior a 'to'." });

            var query = _context.CashSessions
                .Include(s => s.CashRegister)
                .Include(s => s.User)
                .AsQueryable();

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            if (cashRegisterId is not null)
                query = query.Where(s => s.CashRegisterId == cashRegisterId);

            if (isOpen is not null)
                query = isOpen.Value
                    ? query.Where(s => s.ClosedAt == null)
                    : query.Where(s => s.ClosedAt != null);

            if (from is not null)
                query = query.Where(s => s.OpenedAt >= from.Value.Date);

            if (to is not null)
                query = query.Where(s => s.OpenedAt < to.Value.Date.AddDays(1));

            var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);
            var totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(s => s.OpenedAt)
                .ApplyPage(normalizedPage, normalizedPageSize)
                .Select(s => MapToResponse(s))
                .ToListAsync();

            return Ok(sessions.ToPagedResponse(normalizedPage, normalizedPageSize, totalCount));
        }

        /// <summary>
        /// Gets the authenticated user's own currently open session, if any.
        /// </summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(CashSessionResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CashSessionResponse>> GetCurrent()
        {
            var userId = GetUserId();

            var session = await _context.CashSessions
                .Include(s => s.CashRegister)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ClosedAt == null);

            if (session is null)
                return NotFound(new { message = "No hay un turno de caja abierto." });

            return Ok(MapToResponse(session));
        }

        /// <summary>
        /// Gets a session by ID. System administrators can access sessions from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CashSessionResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CashSessionResponse>> GetById(int id)
        {
            var query = _context.CashSessions
                .Include(s => s.CashRegister)
                .Include(s => s.User)
                .Where(s => s.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var session = await query.FirstOrDefaultAsync();

            if (session is null)
                return NotFound(new { message = "Turno no encontrado." });

            return Ok(MapToResponse(session));
        }

        /// <summary>
        /// Opens a new shift for the authenticated user on a cash register. Fails if that
        /// register already has an open session, or if the user already has one open
        /// elsewhere (a cashier can only run one register at a time).
        /// </summary>
        [HttpPost("open")]
        [ProducesResponseType(typeof(CashSessionResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<CashSessionResponse>> Open([FromBody] OpenCashSessionRequest request)
        {
            var userId = GetUserId();
            var currentUser = GetCurrentUsername();

            var register = await _context.CashRegisters
                .FirstOrDefaultAsync(r => r.Id == request.CashRegisterId && r.IsActive);
            if (register is null)
                return BadRequest(new { message = "Caja inválida." });

            if (!IsSystemAdmin() && register.CompanyId != GetCompanyId())
                return StatusCode(403, new { message = "Solo puedes abrir un turno en una caja de tu propia empresa." });

            var registerHasOpenSession = await _context.CashSessions
                .AnyAsync(s => s.CashRegisterId == request.CashRegisterId && s.ClosedAt == null);
            if (registerHasOpenSession)
                return BadRequest(new { message = "Esta caja ya tiene un turno abierto." });

            var userHasOpenSession = await _context.CashSessions
                .AnyAsync(s => s.UserId == userId && s.ClosedAt == null);
            if (userHasOpenSession)
                return BadRequest(new { message = "Ya tienes un turno de caja abierto. Ciérralo antes de abrir otro." });

            var now = DateTimeHelper.Now;
            var session = new CashSession
            {
                CashRegisterId = request.CashRegisterId,
                UserId = userId,
                CompanyId = register.CompanyId,
                OpeningAmount = request.OpeningAmount,
                OpenedAt = now,
                CreatedAt = now,
                CreatedBy = currentUser
            };

            _context.CashSessions.Add(session);
            await _context.SaveChangesAsync();
            await LoadRelations(session);

            _logger.LogInformation("Cash session opened: {Id} on register {RegisterId} by {CreatedBy}",
                session.Id, register.Id, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = session.Id }, MapToResponse(session));
        }

        /// <summary>
        /// Closes a session, recording the counted cash and computing the difference against
        /// what was expected. Any authenticated user in the company can close a session
        /// (e.g. a manager closing out a cashier's shift), not just the one who opened it.
        /// </summary>
        [HttpPost("{id:int}/close")]
        [ProducesResponseType(typeof(CashSessionResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CashSessionResponse>> Close(int id, [FromBody] CloseCashSessionRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.CashSessions.Where(s => s.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var session = await query.FirstOrDefaultAsync();

            if (session is null)
                return NotFound(new { message = "Turno no encontrado." });

            if (session.ClosedAt is not null)
                return BadRequest(new { message = "Este turno ya está cerrado." });

            var cashSalesTotal = await _context.Payments
                .Where(p => p.Method == "Cash" && p.Sale.CashSessionId == session.Id && p.Sale.Status != "Cancelled")
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
            var cashRefundsTotal = await _context.Returns
                .Where(r => r.RefundMethod == "Cash" && r.CashSessionId == session.Id)
                .SumAsync(r => (decimal?)r.TotalRefunded) ?? 0m;
            var expectedAmount = session.OpeningAmount + cashSalesTotal - cashRefundsTotal;

            session.ClosingAmount = request.ClosingAmount;
            session.ExpectedAmount = expectedAmount;
            session.Difference = request.ClosingAmount - expectedAmount;
            session.ClosedAt = DateTimeHelper.Now;
            session.Notes = request.Notes;
            session.ModifiedAt = DateTimeHelper.Now;
            session.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();
            await LoadRelations(session);

            _logger.LogInformation("Cash session closed: {Id} by {ModifiedBy}, difference {Difference}",
                id, currentUser, session.Difference);

            return Ok(MapToResponse(session));
        }

        private async Task LoadRelations(CashSession session)
        {
            await _context.Entry(session).Reference(s => s.CashRegister).LoadAsync();
            await _context.Entry(session).Reference(s => s.User).LoadAsync();
        }

        private static CashSessionResponse MapToResponse(CashSession session)
        {
            return new CashSessionResponse
            {
                Id = session.Id,
                CashRegisterId = session.CashRegisterId,
                CashRegisterName = session.CashRegister.Name,
                UserId = session.UserId,
                Username = session.User.Username,
                OpeningAmount = session.OpeningAmount,
                OpenedAt = session.OpenedAt,
                ClosingAmount = session.ClosingAmount,
                ExpectedAmount = session.ExpectedAmount,
                Difference = session.Difference,
                ClosedAt = session.ClosedAt,
                IsOpen = session.ClosedAt is null,
                Notes = session.Notes,
                CompanyId = session.CompanyId,
                CreatedAt = session.CreatedAt,
                CreatedBy = session.CreatedBy
            };
        }
    }
}
