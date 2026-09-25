using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Suppliers;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for suppliers — regular vendors and consignors alike. See
    /// Supplier.IsConsignor and Product.SupplierId/CommissionType/CommissionValue for how a
    /// consignment relationship actually affects sales.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuppliersController> _logger;

        public SuppliersController(ApplicationDbContext context, ILogger<SuppliersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active suppliers for the authenticated user's company.
        /// System administrators see suppliers across every company.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<SupplierResponse>), 200)]
        public async Task<ActionResult<List<SupplierResponse>>> GetAll()
        {
            var query = _context.Suppliers.Where(s => s.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var suppliers = await query
                .OrderBy(s => s.Name)
                .Select(s => MapToResponse(s))
                .ToListAsync();

            return Ok(suppliers);
        }

        /// <summary>
        /// Gets a supplier by ID. System administrators can access suppliers from any company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SupplierResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SupplierResponse>> GetById(int id)
        {
            var query = _context.Suppliers.Where(s => s.Id == id);

            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var supplier = await query.FirstOrDefaultAsync();

            if (supplier is null)
                return NotFound(new { message = "Proveedor no encontrado." });

            return Ok(MapToResponse(supplier));
        }

        /// <summary>
        /// Creates a new supplier. System administrators may pass a CompanyId to create
        /// it directly in another company; anyone else always creates within their own.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SupplierResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<SupplierResponse>> Create([FromBody] CreateSupplierRequest request)
        {
            var callerCompanyId = GetCompanyId();
            var currentUser = GetCurrentUsername();

            var companyId = callerCompanyId;
            if (request.CompanyId is not null && request.CompanyId != callerCompanyId)
            {
                if (!IsSystemAdmin())
                    return StatusCode(403, new { message = "Solo el administrador del sistema puede crear proveedores en otra empresa." });

                companyId = request.CompanyId.Value;
            }

            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);
            if (!companyExists)
                return BadRequest(new { message = "Empresa inválida." });

            var nameExists = await _context.Suppliers
                .AnyAsync(s => s.Name == request.Name && s.CompanyId == companyId && s.IsActive);
            if (nameExists)
                return BadRequest(new { message = "Ya existe un proveedor con este nombre." });

            var supplier = new Supplier
            {
                Name = request.Name,
                TaxId = request.TaxId,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                IsConsignor = request.IsConsignor,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTimeHelper.Now,
                CreatedBy = currentUser
            };

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Supplier created: {Id} - {Name} by {CreatedBy}", supplier.Id, supplier.Name, currentUser);

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, MapToResponse(supplier));
        }

        /// <summary>
        /// Updates an existing supplier. System administrators can update suppliers
        /// from any company.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(SupplierResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SupplierResponse>> Update(int id, [FromBody] UpdateSupplierRequest request)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Suppliers.Where(s => s.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var supplier = await query.FirstOrDefaultAsync();

            if (supplier is null)
                return NotFound(new { message = "Proveedor no encontrado." });

            var nameExists = await _context.Suppliers
                .AnyAsync(s => s.Name == request.Name && s.CompanyId == supplier.CompanyId && s.Id != id && s.IsActive);
            if (nameExists)
                return BadRequest(new { message = "Ya existe un proveedor con este nombre." });

            if (supplier.IsConsignor && !request.IsConsignor)
            {
                var hasAttributedProducts = await _context.Products
                    .AnyAsync(p => p.SupplierId == id && p.IsActive);
                if (hasAttributedProducts)
                    return BadRequest(new { message = "No se puede quitar la marca de consignador mientras tenga productos activos atribuidos a él." });
            }

            supplier.Name = request.Name;
            supplier.TaxId = request.TaxId;
            supplier.Phone = request.Phone;
            supplier.Email = request.Email;
            supplier.Address = request.Address;
            supplier.IsConsignor = request.IsConsignor;
            supplier.ModifiedAt = DateTimeHelper.Now;
            supplier.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Supplier updated: {Id} - {Name} by {ModifiedBy}", supplier.Id, supplier.Name, currentUser);

            return Ok(MapToResponse(supplier));
        }

        /// <summary>
        /// Deactivates a supplier (soft delete). System administrators can deactivate
        /// suppliers from any company. Refuses if any active product still uses it.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var currentUser = GetCurrentUsername();

            var query = _context.Suppliers.Where(s => s.Id == id);
            if (!IsSystemAdmin())
                query = query.Where(s => s.CompanyId == GetCompanyId());

            var supplier = await query.FirstOrDefaultAsync();

            if (supplier is null)
                return NotFound(new { message = "Proveedor no encontrado." });

            var productsWithSupplier = await _context.Products
                .AnyAsync(p => p.SupplierId == id && p.IsActive);
            if (productsWithSupplier)
                return BadRequest(new { message = "No se puede desactivar el proveedor. Hay productos activos atribuidos a él." });

            supplier.IsActive = false;
            supplier.ModifiedAt = DateTimeHelper.Now;
            supplier.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Supplier deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Proveedor desactivado correctamente." });
        }

        private static SupplierResponse MapToResponse(Supplier supplier)
        {
            return new SupplierResponse
            {
                Id = supplier.Id,
                Name = supplier.Name,
                TaxId = supplier.TaxId,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                IsConsignor = supplier.IsConsignor,
                CompanyId = supplier.CompanyId,
                IsActive = supplier.IsActive,
                CreatedAt = supplier.CreatedAt,
                CreatedBy = supplier.CreatedBy,
                ModifiedAt = supplier.ModifiedAt,
                ModifiedBy = supplier.ModifiedBy
            };
        }
    }
}
