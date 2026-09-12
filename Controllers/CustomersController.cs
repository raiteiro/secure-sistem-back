using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Data;
using SecureSistem.DTOs.Customers;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for customers. Attaching a customer to a sale is optional —
    /// sales can be made to a walk-in "público en general" without one.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ApplicationDbContext context, ILogger<CustomersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active customers for the authenticated user's company.
        /// System administrators see customers across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CustomerResponse>), 200)]
        public async Task<ActionResult<List<CustomerResponse>>> GetAll()
        {
            var query = _context.Customers.Where(c => c.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var customers = await query
                .OrderBy(c => c.Name)
                .Select(c => MapToResponse(c))
                .ToListAsync();

            return Ok(customers);
        }

        /// <summary>
        /// Gets a customer by ID. System administrators can access customers from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CustomerResponse>> GetById(int id)
        {
            var query = _context.Customers.Where(c => c.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var customer = await query.FirstOrDefaultAsync();

            if (customer is null)
                return NotFound(new { message = "Customer not found." });

            return Ok(MapToResponse(customer));
        }

        /// <summary>
        /// Creates a new customer. System administrators may pass a CompanyId to create
        /// it directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Only the system administrator can create customers in another company." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Invalid company." });

            if (request.Email is not null)
            {
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.Email == request.Email && c.CompanyId == companyId && c.IsActive);
                if (emailExists)
                    return BadRequest(new { message = "A customer with this email already exists." });
            }

            var customer = new Customer
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                TaxId = request.TaxId,
                Address = request.Address,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer created: {Id} - {Name} by {CreatedBy}", customer.Id, customer.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, MapToResponse(customer));
        }

        /// <summary>
        /// Updates an existing customer. System administrators can update customers
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(CustomerResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CustomerResponse>> Update(int id, [FromBody] UpdateCustomerRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Customers.Where(c => c.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var customer = await query.FirstOrDefaultAsync();

            if (customer is null)
                return NotFound(new { message = "Customer not found." });

            if (request.Email is not null)
            {
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.Email == request.Email && c.CompanyId == customer.CompanyId && c.Id != id && c.IsActive);
                if (emailExists)
                    return BadRequest(new { message = "A customer with this email already exists." });
            }

            customer.Name = request.Name;
            customer.Email = request.Email;
            customer.Phone = request.Phone;
            customer.TaxId = request.TaxId;
            customer.Address = request.Address;
            customer.ModifiedAt = DateTime.UtcNow;
            customer.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer updated: {Id} - {Name} by {ModifiedBy}", customer.Id, customer.Name, currentUser);

            return Ok(MapToResponse(customer));
        }

        /// <summary>
        /// Deactivates a customer (soft delete). System administrators can deactivate
        /// customers from any company.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Customers.Where(c => c.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(c => c.CompanyId == GetCompanyId());

            var customer = await query.FirstOrDefaultAsync();

            if (customer is null)
                return NotFound(new { message = "Customer not found." });

            customer.IsActive = false;
            customer.ModifiedAt = DateTime.UtcNow;
            customer.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Customer deactivated successfully." });
        }

        private static CustomerResponse MapToResponse(Customer customer)
        {
            return new CustomerResponse
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                TaxId = customer.TaxId,
                Address = customer.Address,
                CompanyId = customer.CompanyId,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt,
                CreatedBy = customer.CreatedBy,
                ModifiedAt = customer.ModifiedAt,
                ModifiedBy = customer.ModifiedBy
            };
        }
    }
}
