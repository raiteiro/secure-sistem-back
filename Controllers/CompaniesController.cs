using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureSistem.Common;
using SecureSistem.Data;
using SecureSistem.DTOs.Companies;
using SecureSistem.Models;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// CRUD operations for company (tenant) management. Creating, updating and deactivating
    /// companies — including their plan limits and subscription status — is restricted to
    /// system administrators. Regular users can only view their own company.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : BaseApiController
    {
        private static readonly Dictionary<string, string> AllowedLogoContentTypes = new()
        {
            ["image/png"] = ".png",
            ["image/jpeg"] = ".jpg",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif"
        };
        private const long MaxLogoSizeBytes = 2 * 1024 * 1024; // 2 MB

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<CompaniesController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active companies. System administrators see every company;
        /// regular users see only their own.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CompanyResponse>), 200)]
        public async Task<ActionResult<List<CompanyResponse>>> GetAll()
        {
            var query = _context.Companies.Where(c => c.IsActive);

            if (!IsSystemAdmin())
                query = query.Where(c => c.Id == GetCompanyId());

            var companies = await query
                .OrderBy(c => c.Name)
                .Select(c => new CompanyResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    TaxId = c.TaxId,
                    Address = c.Address,
                    Phone = c.Phone,
                    Email = c.Email,
                    Website = c.Website,
                    Instagram = c.Instagram,
                    Facebook = c.Facebook,
                    TikTok = c.TikTok,
                    WhatsApp = c.WhatsApp,
                    LogoPath = c.LogoPath,
                    ColorPreset = c.ColorPreset,
                    IsActive = c.IsActive,
                    MaxUsers = c.MaxUsers,
                    MaxConcurrentSessions = c.MaxConcurrentSessions,
                    SubscriptionExpiresAt = c.SubscriptionExpiresAt,
                    ActiveUserCount = _context.Users.Count(u => u.CompanyId == c.Id && u.IsActive),
                    ActiveSessionCount = _context.RefreshTokens.Count(rt =>
                        rt.User.CompanyId == c.Id && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow),
                    CreatedAt = c.CreatedAt,
                    CreatedBy = c.CreatedBy,
                    ModifiedAt = c.ModifiedAt,
                    ModifiedBy = c.ModifiedBy
                })
                .ToListAsync();

            return Ok(companies);
        }

        /// <summary>
        /// Gets a company by ID. Regular users can only access their own company.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CompanyResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CompanyResponse>> GetById(int id)
        {
            if (!IsSystemAdmin() && id != GetCompanyId())
                return NotFound(new { message = "Empresa no encontrada." });

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company is null)
                return NotFound(new { message = "Empresa no encontrada." });

            return Ok(await MapToResponseAsync(company));
        }

        /// <summary>
        /// Creates a new company. System administrator only. Also bootstraps the company
        /// with the standard system navigation routes (Dashboard + Administration menu),
        /// a default administrator role with access to all of them, and a default
        /// administrator user (forced to change their password on first login).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CompanyResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<CompanyResponse>> Create([FromBody] CreateCompanyRequest request)
        {
            if (!IsSystemAdmin())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede crear empresas." });

            var currentUser = GetCurrentUsername();

            var nameExists = await _context.Companies.AnyAsync(c => c.Name == request.Name && c.IsActive);
            if (nameExists)
                return BadRequest(new { message = "Ya existe una empresa con este nombre." });

            var slug = Regex.Replace(request.Name, "[^a-zA-Z0-9]", "").ToLowerInvariant();
            if (slug.Length == 0)
                return BadRequest(new { message = "El nombre de la empresa debe contener al menos una letra o dígito." });

            var adminUsername = $"admin{slug}";
            var adminEmail = $"{adminUsername}@{slug}.local";
            var emailExists = await _context.Users.AnyAsync(u => u.Email == adminEmail);
            if (emailExists)
                return BadRequest(new
                {
                    message = "No se pudo generar una cuenta de administrador por defecto única para este nombre de empresa. Intenta con un nombre más distintivo."
                });

            using var transaction = await _context.Database.BeginTransactionAsync();
            var now = DateTimeHelper.Now;

            var company = new Company
            {
                Name = request.Name,
                TaxId = request.TaxId,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Website = request.Website,
                Instagram = request.Instagram,
                Facebook = request.Facebook,
                TikTok = request.TikTok,
                WhatsApp = request.WhatsApp,
                ColorPreset = request.ColorPreset,
                MaxUsers = request.MaxUsers,
                MaxConcurrentSessions = request.MaxConcurrentSessions,
                SubscriptionExpiresAt = request.SubscriptionExpiresAt,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Every company needs at least one branch to operate the point of sale.
            var mainBranch = new Branch
            {
                Name = "Sucursal Principal",
                Address = request.Address,
                Phone = request.Phone,
                CompanyId = company.Id,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.Branches.Add(mainBranch);
            await _context.SaveChangesAsync();

            // Every company needs at least one place to hold stock.
            var mainWarehouse = new Warehouse
            {
                Name = $"Almacén {mainBranch.Name}",
                Address = request.Address,
                BranchId = mainBranch.Id,
                CompanyId = company.Id,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.Warehouses.Add(mainWarehouse);
            await _context.SaveChangesAsync();

            // Standard system pages every company gets by default.
            var dashboard = new NavigationRoute
            {
                WindowName = "Dashboard", RoutePath = "/dashboard", Icon = "fas fa-home",
                WindowId = "win-dashboard", Level = 0, SortOrder = 1,
                CompanyId = company.Id, IsActive = true, CreatedAt = now, CreatedBy = currentUser
            };
            var administration = new NavigationRoute
            {
                WindowName = "Administración", RoutePath = "#", Icon = "fas fa-cogs",
                WindowId = "win-admin", Level = 0, SortOrder = 2,
                CompanyId = company.Id, IsActive = true, CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.AddRange(dashboard, administration);
            await _context.SaveChangesAsync();

            var users = new NavigationRoute
            {
                ParentId = administration.Id, WindowName = "Usuarios", RoutePath = "/admin/users",
                Icon = "fas fa-users", WindowId = "win-users", Level = 1, SortOrder = 1,
                CompanyId = company.Id, IsActive = true, CreatedAt = now, CreatedBy = currentUser
            };
            var roles = new NavigationRoute
            {
                ParentId = administration.Id, WindowName = "Roles", RoutePath = "/admin/roles",
                Icon = "fas fa-user-tag", WindowId = "win-roles", Level = 1, SortOrder = 2,
                CompanyId = company.Id, IsActive = true, CreatedAt = now, CreatedBy = currentUser
            };
            var routes = new NavigationRoute
            {
                ParentId = administration.Id, WindowName = "Rutas", RoutePath = "/admin/routes",
                Icon = "fas fa-sitemap", WindowId = "win-routes", Level = 1, SortOrder = 3,
                CompanyId = company.Id, IsActive = true, CreatedAt = now, CreatedBy = currentUser
            };
            var companies = new NavigationRoute
            {
                ParentId = administration.Id, WindowName = "Empresas", RoutePath = "/admin/empresas",
                Icon = "fas fa-building", WindowId = "win-company", Level = 1, SortOrder = 4,
                CompanyId = company.Id, IsActive = true, CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.AddRange(users, roles, routes, companies);
            await _context.SaveChangesAsync();

            // POS module pages: core to this system, so every role (present and future)
            // gets them by default — IsDefaultForNewRoles = true.
            var catalog = new NavigationRoute
            {
                WindowName = "Catálogo", RoutePath = "#", Icon = "fas fa-boxes-stacked",
                WindowId = "win-catalog", Level = 0, SortOrder = 3,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(catalog);
            await _context.SaveChangesAsync();

            var branchesRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Sucursales", RoutePath = "/catalogo/sucursales",
                Icon = "fas fa-store", WindowId = "win-branches", Level = 1, SortOrder = 1,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var warehousesRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Almacenes", RoutePath = "/catalogo/almacenes",
                Icon = "fas fa-warehouse", WindowId = "win-warehouses", Level = 1, SortOrder = 2,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var categoriesRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Categorías", RoutePath = "/catalogo/categorias",
                Icon = "fas fa-tags", WindowId = "win-categories", Level = 1, SortOrder = 3,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var productsRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Productos", RoutePath = "/catalogo/productos",
                Icon = "fas fa-box", WindowId = "win-products", Level = 1, SortOrder = 4,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var taxRatesRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Impuestos", RoutePath = "/catalogo/impuestos",
                Icon = "fas fa-percent", WindowId = "win-taxrates", Level = 1, SortOrder = 5,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var inventoryRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Inventario", RoutePath = "/catalogo/inventario",
                Icon = "fas fa-clipboard-list", WindowId = "win-inventory", Level = 1, SortOrder = 6,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var customersRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Clientes", RoutePath = "/catalogo/clientes",
                Icon = "fas fa-address-book", WindowId = "win-customers", Level = 1, SortOrder = 7,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var suppliersRoute = new NavigationRoute
            {
                ParentId = catalog.Id, WindowName = "Proveedores", RoutePath = "/catalogo/proveedores",
                Icon = "fas fa-truck", WindowId = "win-suppliers", Level = 1, SortOrder = 8,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.AddRange(
                branchesRoute, warehousesRoute, categoriesRoute, productsRoute, taxRatesRoute, inventoryRoute, customersRoute, suppliersRoute);
            await _context.SaveChangesAsync();

            // Cash register operations: a separate top-level group from "Catálogo", since
            // it's daily operational workflow rather than reference/catalog data.
            var cash = new NavigationRoute
            {
                WindowName = "Caja", RoutePath = "#", Icon = "fas fa-cash-register",
                WindowId = "win-cash", Level = 0, SortOrder = 4,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(cash);
            await _context.SaveChangesAsync();

            var cashRegistersRoute = new NavigationRoute
            {
                ParentId = cash.Id, WindowName = "Cajas", RoutePath = "/caja/cajas",
                Icon = "fas fa-cash-register", WindowId = "win-cashregisters", Level = 1, SortOrder = 1,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            var cashSessionsRoute = new NavigationRoute
            {
                ParentId = cash.Id, WindowName = "Turnos", RoutePath = "/caja/turnos",
                Icon = "fas fa-clock", WindowId = "win-cashsessions", Level = 1, SortOrder = 2,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.AddRange(cashRegistersRoute, cashSessionsRoute);
            await _context.SaveChangesAsync();

            // The POS checkout screen itself: a single page, not a group.
            var sales = new NavigationRoute
            {
                WindowName = "Ventas", RoutePath = "/ventas", Icon = "fas fa-receipt",
                WindowId = "win-sales", Level = 0, SortOrder = 5,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(sales);
            await _context.SaveChangesAsync();

            // Returns/cancellations screen: also a single page, next to "Ventas".
            var returns = new NavigationRoute
            {
                WindowName = "Devoluciones", RoutePath = "/devoluciones", Icon = "fas fa-rotate-left",
                WindowId = "win-returns", Level = 0, SortOrder = 6,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(returns);
            await _context.SaveChangesAsync();

            // Reporting screen: also a single page, next to "Devoluciones".
            var reports = new NavigationRoute
            {
                WindowName = "Reportes", RoutePath = "/reportes", Icon = "fas fa-chart-line",
                WindowId = "win-reports", Level = 0, SortOrder = 7,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(reports);
            await _context.SaveChangesAsync();

            // Consignment settlements: also a single page, next to "Reportes".
            var consignment = new NavigationRoute
            {
                WindowName = "Consignaciones", RoutePath = "/consignaciones", Icon = "fas fa-handshake",
                WindowId = "win-consignment", Level = 0, SortOrder = 8,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(consignment);
            await _context.SaveChangesAsync();

            // Quotes: also a single page, next to "Consignaciones".
            var quotes = new NavigationRoute
            {
                WindowName = "Cotizaciones", RoutePath = "/cotizaciones", Icon = "fas fa-file-invoice",
                WindowId = "win-quotes", Level = 0, SortOrder = 9,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(quotes);
            await _context.SaveChangesAsync();

            // Purchase orders: also a single page, next to "Cotizaciones".
            var purchaseOrders = new NavigationRoute
            {
                WindowName = "Órdenes de compra", RoutePath = "/ordenes-compra", Icon = "fas fa-dolly",
                WindowId = "win-purchaseorders", Level = 0, SortOrder = 10,
                CompanyId = company.Id, IsActive = true, IsDefaultForNewRoles = true,
                CreatedAt = now, CreatedBy = currentUser
            };
            _context.NavigationRoutes.Add(purchaseOrders);
            await _context.SaveChangesAsync();

            var allRoutes = new[]
            {
                dashboard, administration, users, roles, routes, companies, sales, returns, reports, consignment, quotes, purchaseOrders,
                catalog, branchesRoute, warehousesRoute, categoriesRoute, productsRoute, taxRatesRoute, inventoryRoute, customersRoute, suppliersRoute,
                cash, cashRegistersRoute, cashSessionsRoute
            };

            var adminRole = new Role
            {
                Name = $"admin{request.Name}",
                Description = "Administrador de la empresa, con acceso a todas las páginas del sistema.",
                CompanyId = company.Id,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.Roles.Add(adminRole);
            await _context.SaveChangesAsync();

            foreach (var route in allRoutes)
            {
                _context.RoleNavigationRoutes.Add(new RoleNavigationRoute
                {
                    RoleId = adminRole.Id,
                    NavigationRouteId = route.Id,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = currentUser
                });
            }

            // Permission catalog is global (not per-company), unlike routes.
            var defaultPermissionIds = await _context.Permissions
                .Where(p => p.IsActive && p.IsDefaultForNewRoles)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var permissionId in defaultPermissionIds)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permissionId,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = currentUser
                });
            }

            var adminUser = new User
            {
                Username = adminUsername,
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminUsername),
                FirstName = "Admin",
                LastName = request.Name,
                RoleId = adminRole.Id,
                CompanyId = company.Id,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = now,
                CreatedBy = currentUser
            };
            _context.Users.Add(adminUser);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Company created: {Id} - {Name} by {CreatedBy}, with default role '{Role}' and admin user '{Username}'",
                company.Id, company.Name, currentUser, adminRole.Name, adminUser.Username);

            return CreatedAtAction(nameof(GetById), new { id = company.Id }, await MapToResponseAsync(company));
        }

        /// <summary>
        /// Updates a company's profile and plan limits. System administrator only.
        /// </summary>
        [HttpPost("{id:int}/update")]
        [ProducesResponseType(typeof(CompanyResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CompanyResponse>> Update(int id, [FromBody] UpdateCompanyRequest request)
        {
            if (!IsSystemAdmin())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede actualizar empresas." });

            var currentUser = GetCurrentUsername();

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company is null)
                return NotFound(new { message = "Empresa no encontrada." });

            var nameExists = await _context.Companies
                .AnyAsync(c => c.Name == request.Name && c.Id != id && c.IsActive);
            if (nameExists)
                return BadRequest(new { message = "Ya existe una empresa con este nombre." });

            company.Name = request.Name;
            company.TaxId = request.TaxId;
            company.Address = request.Address;
            company.Phone = request.Phone;
            company.Email = request.Email;
            company.Website = request.Website;
            company.Instagram = request.Instagram;
            company.Facebook = request.Facebook;
            company.TikTok = request.TikTok;
            company.WhatsApp = request.WhatsApp;
            company.ColorPreset = request.ColorPreset;
            company.MaxUsers = request.MaxUsers;
            company.MaxConcurrentSessions = request.MaxConcurrentSessions;
            company.SubscriptionExpiresAt = request.SubscriptionExpiresAt;
            company.ModifiedAt = DateTimeHelper.Now;
            company.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Company updated: {Id} - {Name} by {ModifiedBy}", company.Id, company.Name, currentUser);

            return Ok(await MapToResponseAsync(company));
        }

        /// <summary>
        /// Deactivates a company (soft delete). System administrator only.
        /// </summary>
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            if (!IsSystemAdmin())
                return StatusCode(403, new { message = "Solo el administrador del sistema puede desactivar empresas." });

            var currentUser = GetCurrentUsername();

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company is null)
                return NotFound(new { message = "Empresa no encontrada." });

            company.IsActive = false;
            company.ModifiedAt = DateTimeHelper.Now;
            company.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Company deactivated: {Id} by {ModifiedBy}", id, currentUser);

            return Ok(new { message = "Empresa desactivada correctamente." });
        }

        /// <summary>
        /// Uploads (or replaces) a company's logo image, used on generated PDF reports.
        /// System administrators can update any company's logo; everyone else only their own.
        /// Accepts PNG, JPEG, WEBP or GIF, up to 2 MB.
        /// </summary>
        [HttpPost("{id:int}/logo")]
        [ProducesResponseType(typeof(CompanyResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CompanyResponse>> UploadLogo(int id, IFormFile file)
        {
            if (!IsSystemAdmin() && id != GetCompanyId())
                return StatusCode(403, new { message = "Solo puedes actualizar el logo de tu propia empresa." });

            var currentUser = GetCurrentUsername();

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company is null)
                return NotFound(new { message = "Empresa no encontrada." });

            if (file is null || file.Length == 0)
                return BadRequest(new { message = "No se subió ningún archivo." });

            if (file.Length > MaxLogoSizeBytes)
                return BadRequest(new { message = "El logo debe pesar 2 MB o menos." });

            if (!AllowedLogoContentTypes.TryGetValue(file.ContentType, out var extension))
                return BadRequest(new { message = "El logo debe ser una imagen PNG, JPEG, WEBP o GIF." });

            var folderRelative = Path.Combine("uploads", "companies", id.ToString());
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var folderAbsolute = Path.Combine(webRoot, folderRelative);
            Directory.CreateDirectory(folderAbsolute);

            // Remove any previously uploaded logo (it may have a different extension).
            foreach (var existingFile in Directory.EnumerateFiles(folderAbsolute, "logo.*"))
                System.IO.File.Delete(existingFile);

            var fileName = $"logo{extension}";
            var fileAbsolutePath = Path.Combine(folderAbsolute, fileName);

            using (var stream = new FileStream(fileAbsolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            company.LogoPath = $"/{folderRelative.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
            company.ModifiedAt = DateTimeHelper.Now;
            company.ModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Logo uploaded for company {Id} by {ModifiedBy}", id, currentUser);

            return Ok(await MapToResponseAsync(company));
        }

        /// <summary>
        /// Updates only a company's color preset (theme), separate from the rest of the
        /// profile/plan fields. System administrators can update any company; everyone
        /// else only their own.
        /// </summary>
        [HttpPost("{id:int}/color-preset")]
        [ProducesResponseType(typeof(CompanyResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CompanyResponse>> UpdateColorPreset(int id, [FromBody] UpdateColorPresetRequest request)
        {
            if (!IsSystemAdmin() && id != GetCompanyId())
                return StatusCode(403, new { message = "Solo puedes actualizar el tema de color de tu propia empresa." });

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company is null)
                return NotFound(new { message = "Empresa no encontrada." });

            company.ColorPreset = request.ColorPreset;
            company.ModifiedAt = DateTimeHelper.Now;
            company.ModifiedBy = GetCurrentUsername();

            await _context.SaveChangesAsync();

            _logger.LogInformation("Color preset updated for company {Id} by {ModifiedBy}", id, company.ModifiedBy);

            return Ok(await MapToResponseAsync(company));
        }

        private async Task<CompanyResponse> MapToResponseAsync(Company company)
        {
            var activeUserCount = await _context.Users
                .CountAsync(u => u.CompanyId == company.Id && u.IsActive);
            var activeSessionCount = await _context.RefreshTokens
                .CountAsync(rt => rt.User.CompanyId == company.Id
                    && rt.RevokedAt == null
                    && rt.ExpiresAt > DateTime.UtcNow);

            return new CompanyResponse
            {
                Id = company.Id,
                Name = company.Name,
                TaxId = company.TaxId,
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                Website = company.Website,
                Instagram = company.Instagram,
                Facebook = company.Facebook,
                TikTok = company.TikTok,
                WhatsApp = company.WhatsApp,
                LogoPath = company.LogoPath,
                ColorPreset = company.ColorPreset,
                IsActive = company.IsActive,
                MaxUsers = company.MaxUsers,
                MaxConcurrentSessions = company.MaxConcurrentSessions,
                SubscriptionExpiresAt = company.SubscriptionExpiresAt,
                ActiveUserCount = activeUserCount,
                ActiveSessionCount = activeSessionCount,
                CreatedAt = company.CreatedAt,
                CreatedBy = company.CreatedBy,
                ModifiedAt = company.ModifiedAt,
                ModifiedBy = company.ModifiedBy
            };
        }
    }
}
