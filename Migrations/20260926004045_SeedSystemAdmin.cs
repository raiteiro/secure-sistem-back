using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <summary>
    /// Seeds the first system administrator so a fresh database can be logged into right
    /// after <c>dotnet ef database update</c>. Creates a dedicated company with the same
    /// bootstrap <c>CompaniesController.Create</c> gives every new company (main branch,
    /// main warehouse, full route tree, admin role with every default permission), plus a
    /// <c>sysadmin</c> user flagged <c>IsSystemAdmin = 1</c>.
    ///
    /// Only runs when the database has no system administrator yet, so it never touches
    /// databases where one was already set up by hand.
    ///
    /// Initial credentials: <c>sysadmin</c> / <c>Admin123!</c>, with
    /// <c>MustChangePassword = 1</c> so the password must be replaced on first login.
    /// </summary>
    public partial class SeedSystemAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [IsSystemAdmin] = 1)
BEGIN
    -- Business timestamps are stored as naive Mexico City time (see Common/DateTimeHelper.cs).
    DECLARE @now datetime2 = CONVERT(datetime2, SYSDATETIMEOFFSET() AT TIME ZONE 'Central Standard Time (Mexico)');
    DECLARE @by nvarchar(100) = N'system';
    DECLARE @companyId int, @branchId int, @roleId int;
    DECLARE @administrationId int, @catalogId int, @cashId int;

    INSERT INTO [Companies] ([Name], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (N'Administración del Sistema', 1, @now, @by);
    SET @companyId = SCOPE_IDENTITY();

    INSERT INTO [Branches] ([Name], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (N'Sucursal Principal', @companyId, 1, @now, @by);
    SET @branchId = SCOPE_IDENTITY();

    INSERT INTO [Warehouses] ([Name], [BranchId], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (N'Almacén Sucursal Principal', @branchId, @companyId, 1, @now, @by);

    -- Top-level routes (same tree as CompaniesController.Create).
    INSERT INTO [NavigationRoutes] ([ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
    VALUES
        (NULL, N'Dashboard',          N'/dashboard',      N'fas fa-home',          N'win-dashboard',      0, 1,  @companyId, 1, 0, @now, @by),
        (NULL, N'Ventas',             N'/ventas',         N'fas fa-receipt',       N'win-sales',          0, 5,  @companyId, 1, 1, @now, @by),
        (NULL, N'Devoluciones',       N'/devoluciones',   N'fas fa-rotate-left',   N'win-returns',        0, 6,  @companyId, 1, 1, @now, @by),
        (NULL, N'Reportes',           N'/reportes',       N'fas fa-chart-line',    N'win-reports',        0, 7,  @companyId, 1, 1, @now, @by),
        (NULL, N'Consignaciones',     N'/consignaciones', N'fas fa-handshake',     N'win-consignment',    0, 8,  @companyId, 1, 1, @now, @by),
        (NULL, N'Cotizaciones',       N'/cotizaciones',   N'fas fa-file-invoice',  N'win-quotes',         0, 9,  @companyId, 1, 1, @now, @by),
        (NULL, N'Órdenes de compra',  N'/ordenes-compra', N'fas fa-dolly',         N'win-purchaseorders', 0, 10, @companyId, 1, 1, @now, @by);

    INSERT INTO [NavigationRoutes] ([ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
    VALUES (NULL, N'Administración', N'#', N'fas fa-cogs', N'win-admin', 0, 2, @companyId, 1, 0, @now, @by);
    SET @administrationId = SCOPE_IDENTITY();

    INSERT INTO [NavigationRoutes] ([ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
    VALUES (NULL, N'Catálogo', N'#', N'fas fa-boxes-stacked', N'win-catalog', 0, 3, @companyId, 1, 1, @now, @by);
    SET @catalogId = SCOPE_IDENTITY();

    INSERT INTO [NavigationRoutes] ([ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
    VALUES (NULL, N'Caja', N'#', N'fas fa-cash-register', N'win-cash', 0, 4, @companyId, 1, 1, @now, @by);
    SET @cashId = SCOPE_IDENTITY();

    -- Child routes.
    INSERT INTO [NavigationRoutes] ([ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
    VALUES
        (@administrationId, N'Usuarios',    N'/admin/users',           N'fas fa-users',          N'win-users',         1, 1, @companyId, 1, 0, @now, @by),
        (@administrationId, N'Roles',       N'/admin/roles',           N'fas fa-user-tag',       N'win-roles',         1, 2, @companyId, 1, 0, @now, @by),
        (@administrationId, N'Rutas',       N'/admin/routes',          N'fas fa-sitemap',        N'win-routes',        1, 3, @companyId, 1, 0, @now, @by),
        (@administrationId, N'Empresas',    N'/admin/empresas',        N'fas fa-building',       N'win-company',       1, 4, @companyId, 1, 0, @now, @by),
        (@catalogId,        N'Sucursales',  N'/catalogo/sucursales',   N'fas fa-store',          N'win-branches',      1, 1, @companyId, 1, 1, @now, @by),
        (@catalogId,        N'Almacenes',   N'/catalogo/almacenes',    N'fas fa-warehouse',      N'win-warehouses',    1, 2, @companyId, 1, 1, @now, @by),
        (@catalogId,        N'Categorías',  N'/catalogo/categorias',   N'fas fa-tags',           N'win-categories',    1, 3, @companyId, 1, 1, @now, @by),
        (@catalogId,        N'Productos',   N'/catalogo/productos',    N'fas fa-box',            N'win-products',      1, 4, @companyId, 1, 1, @now, @by),
        (@catalogId,        N'Impuestos',   N'/catalogo/impuestos',    N'fas fa-percent',        N'win-taxrates',      1, 5, @companyId, 1, 1, @now, @by),
        (@catalogId,        N'Inventario',  N'/catalogo/inventario',   N'fas fa-clipboard-list', N'win-inventory',     1, 6, @companyId, 1, 1, @now, @by),
        (@catalogId,        N'Clientes',    N'/catalogo/clientes',     N'fas fa-address-book',   N'win-customers',     1, 7, @companyId, 1, 1, @now, @by),
        (@catalogId,        N'Proveedores', N'/catalogo/proveedores',  N'fas fa-truck',          N'win-suppliers',     1, 8, @companyId, 1, 1, @now, @by),
        (@cashId,           N'Cajas',       N'/caja/cajas',            N'fas fa-cash-register',  N'win-cashregisters', 1, 1, @companyId, 1, 1, @now, @by),
        (@cashId,           N'Turnos',      N'/caja/turnos',           N'fas fa-clock',          N'win-cashsessions',  1, 2, @companyId, 1, 1, @now, @by);

    INSERT INTO [Roles] ([Name], [Description], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (N'Administrador del Sistema', N'Administrador del sistema, con acceso a todas las páginas y a la gestión de empresas.', @companyId, 1, @now, @by);
    SET @roleId = SCOPE_IDENTITY();

    INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
    SELECT @roleId, [Id], 1, @now, @by
    FROM [NavigationRoutes]
    WHERE [CompanyId] = @companyId;

    INSERT INTO [RolePermissions] ([RoleId], [PermissionId], [IsActive], [CreatedAt], [CreatedBy])
    SELECT @roleId, [Id], 1, @now, @by
    FROM [Permissions]
    WHERE [IsActive] = 1 AND [IsDefaultForNewRoles] = 1;

    -- Password: Admin123! (BCrypt). Must be changed on first login.
    INSERT INTO [Users] ([Username], [Email], [PasswordHash], [FirstName], [LastName], [RoleId], [CompanyId], [IsActive], [IsSystemAdmin], [MustChangePassword], [CreatedAt], [CreatedBy])
    VALUES (N'sysadmin', N'sysadmin@sistema.local', N'$2a$11$PGeqmbiBUyNR.mP1mlmg7OCYYhMSSygGLHMM/ErNCW6eu5JMVGhQ.', N'Admin', N'Sistema', @roleId, @companyId, 1, 1, 1, @now, @by);
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally a no-op: by the time this is rolled back the seeded company may
            // already own sales, users, etc., and records are never physically deleted
            // (soft delete only). Deactivate the seeded data by hand if it's truly unwanted.
        }
    }
}
