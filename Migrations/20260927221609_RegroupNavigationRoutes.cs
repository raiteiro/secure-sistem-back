using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <summary>
    /// Regroups every company's menu so it isn't a long flat list: new groups "Punto de
    /// venta", "Comercial", "Compras", "Inventario" and "Configuración", with the existing
    /// pages moved under them by WindowId. Paths and WindowIds never change — only ParentId,
    /// Level and SortOrder — so the frontend needs no changes. The old "Catálogo" and "Caja"
    /// groups are deactivated once empty (never deleted). Pages a company created by hand
    /// (any other WindowId) are left where they are. Same structure as
    /// CompaniesController.Create gives new companies.
    /// </summary>
    public partial class RegroupNavigationRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @now datetime2 = CONVERT(datetime2, SYSDATETIMEOFFSET() AT TIME ZONE 'Central Standard Time (Mexico)');

DECLARE @groups TABLE (WindowId nvarchar(100) PRIMARY KEY, Name nvarchar(100), Icon nvarchar(100), SortOrder int);
INSERT INTO @groups VALUES
    (N'win-pos', N'Punto de venta', N'fas fa-cash-register', 2),
    (N'win-commercial', N'Comercial', N'fas fa-briefcase', 3),
    (N'win-purchasing', N'Compras', N'fas fa-cart-flatbed', 4),
    (N'win-stock', N'Inventario', N'fas fa-boxes-stacked', 5),
    (N'win-config', N'Configuración', N'fas fa-sliders', 7);

DECLARE @pages TABLE (WindowId nvarchar(100) PRIMARY KEY, GroupWindowId nvarchar(100), SortOrder int);
INSERT INTO @pages VALUES
    (N'win-sales', N'win-pos', 1),
    (N'win-returns', N'win-pos', 2),
    (N'win-cashregisters', N'win-pos', 3),
    (N'win-cashsessions', N'win-pos', 4),
    (N'win-quotes', N'win-commercial', 1),
    (N'win-customers', N'win-commercial', 2),
    (N'win-purchaseorders', N'win-purchasing', 1),
    (N'win-suppliers', N'win-purchasing', 2),
    (N'win-consignment', N'win-purchasing', 3),
    (N'win-products', N'win-stock', 1),
    (N'win-categories', N'win-stock', 2),
    (N'win-warehouses', N'win-stock', 3),
    (N'win-inventory', N'win-stock', 4),
    (N'win-branches', N'win-config', 1),
    (N'win-taxrates', N'win-config', 2);

-- 1. Each new group, in every company that has at least one of its pages: reactivate it if
--    it already exists (e.g. after a Down), otherwise create it — never a duplicate.
DECLARE @needed TABLE (CompanyId int, WindowId nvarchar(100));
INSERT INTO @needed (CompanyId, WindowId)
SELECT DISTINCT page.[CompanyId], p.GroupWindowId
FROM [NavigationRoutes] page
JOIN @pages p ON p.WindowId = page.[WindowId]
WHERE page.[IsActive] = 1;

UPDATE grp
SET grp.[IsActive] = 1, grp.[WindowName] = g.Name, grp.[Icon] = g.Icon, grp.[SortOrder] = g.SortOrder,
    grp.[ParentId] = NULL, grp.[Level] = 0, grp.[ModifiedAt] = @now, grp.[ModifiedBy] = N'system'
FROM [NavigationRoutes] grp
JOIN @groups g ON g.WindowId = grp.[WindowId]
JOIN @needed n ON n.CompanyId = grp.[CompanyId] AND n.WindowId = grp.[WindowId]
WHERE grp.[IsActive] = 0
  AND grp.[Id] = (SELECT MIN(x.[Id]) FROM [NavigationRoutes] x WHERE x.[CompanyId] = grp.[CompanyId] AND x.[WindowId] = grp.[WindowId])
  AND NOT EXISTS (SELECT 1 FROM [NavigationRoutes] x WHERE x.[CompanyId] = grp.[CompanyId] AND x.[WindowId] = grp.[WindowId] AND x.[IsActive] = 1);

INSERT INTO [NavigationRoutes] ([ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
SELECT NULL, g.Name, N'#', g.Icon, g.WindowId, 0, g.SortOrder, n.CompanyId, 1, 1, @now, N'system'
FROM @needed n
JOIN @groups g ON g.WindowId = n.WindowId
WHERE NOT EXISTS (SELECT 1 FROM [NavigationRoutes] x WHERE x.[CompanyId] = n.CompanyId AND x.[WindowId] = n.WindowId);

-- 2. Move the pages under their group.
UPDATE page
SET page.[ParentId] = grp.[Id], page.[Level] = 1, page.[SortOrder] = p.SortOrder,
    page.[ModifiedAt] = @now, page.[ModifiedBy] = N'system'
FROM [NavigationRoutes] page
JOIN @pages p ON p.WindowId = page.[WindowId]
JOIN [NavigationRoutes] grp ON grp.[CompanyId] = page.[CompanyId] AND grp.[WindowId] = p.GroupWindowId AND grp.[IsActive] = 1
WHERE page.[IsActive] = 1;

-- 3. Top-level order of what stays at the root.
UPDATE r
SET r.[SortOrder] = CASE r.[WindowId] WHEN N'win-dashboard' THEN 1 WHEN N'win-reports' THEN 6 WHEN N'win-admin' THEN 8 END,
    r.[ParentId] = NULL, r.[Level] = 0, r.[ModifiedAt] = @now, r.[ModifiedBy] = N'system'
FROM [NavigationRoutes] r
WHERE r.[WindowId] IN (N'win-dashboard', N'win-reports', N'win-admin') AND r.[IsActive] = 1;

-- 4. Deactivate the old groups once nothing active hangs from them.
UPDATE g
SET g.[IsActive] = 0, g.[ModifiedAt] = @now, g.[ModifiedBy] = N'system'
FROM [NavigationRoutes] g
WHERE g.[WindowId] IN (N'win-catalog', N'win-cash') AND g.[IsActive] = 1
  AND NOT EXISTS (SELECT 1 FROM [NavigationRoutes] c WHERE c.[ParentId] = g.[Id] AND c.[IsActive] = 1);

-- 5. Assign each new group to every active role that already sees one of its pages, so
--    role/route management screens show the group checked (the menu itself already
--    includes a page's parent automatically).
INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
SELECT DISTINCT rnr.[RoleId], grp.[Id], 1, @now, N'system'
FROM [NavigationRoutes] grp
JOIN @groups g ON g.WindowId = grp.[WindowId]
JOIN [NavigationRoutes] page ON page.[ParentId] = grp.[Id] AND page.[IsActive] = 1
JOIN [RoleNavigationRoutes] rnr ON rnr.[NavigationRouteId] = page.[Id] AND rnr.[IsActive] = 1
JOIN [Roles] r ON r.[Id] = rnr.[RoleId] AND r.[IsActive] = 1
WHERE grp.[IsActive] = 1
  AND NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] x WHERE x.[RoleId] = rnr.[RoleId] AND x.[NavigationRouteId] = grp.[Id]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the previous layout: "Catálogo" and "Caja" groups back, the rest of the
            // POS pages as top-level items, and the new groups deactivated.
            migrationBuilder.Sql(@"
DECLARE @now datetime2 = CONVERT(datetime2, SYSDATETIMEOFFSET() AT TIME ZONE 'Central Standard Time (Mexico)');

UPDATE [NavigationRoutes]
SET [IsActive] = 1, [ModifiedAt] = @now, [ModifiedBy] = N'system',
    [SortOrder] = CASE [WindowId] WHEN N'win-catalog' THEN 3 ELSE 4 END
WHERE [WindowId] IN (N'win-catalog', N'win-cash');

DECLARE @pages TABLE (WindowId nvarchar(100) PRIMARY KEY, GroupWindowId nvarchar(100) NULL, SortOrder int);
INSERT INTO @pages VALUES
    (N'win-branches', N'win-catalog', 1), (N'win-warehouses', N'win-catalog', 2),
    (N'win-categories', N'win-catalog', 3), (N'win-products', N'win-catalog', 4),
    (N'win-taxrates', N'win-catalog', 5), (N'win-inventory', N'win-catalog', 6),
    (N'win-customers', N'win-catalog', 7), (N'win-suppliers', N'win-catalog', 8),
    (N'win-cashregisters', N'win-cash', 1), (N'win-cashsessions', N'win-cash', 2),
    (N'win-sales', NULL, 5), (N'win-returns', NULL, 6), (N'win-reports', NULL, 7),
    (N'win-consignment', NULL, 8), (N'win-quotes', NULL, 9), (N'win-purchaseorders', NULL, 10),
    (N'win-dashboard', NULL, 1), (N'win-admin', NULL, 2);

UPDATE page
SET page.[ParentId] = grp.[Id],
    page.[Level] = CASE WHEN p.GroupWindowId IS NULL THEN 0 ELSE 1 END,
    page.[SortOrder] = p.SortOrder, page.[ModifiedAt] = @now, page.[ModifiedBy] = N'system'
FROM [NavigationRoutes] page
JOIN @pages p ON p.WindowId = page.[WindowId]
LEFT JOIN [NavigationRoutes] grp ON grp.[CompanyId] = page.[CompanyId] AND grp.[WindowId] = p.GroupWindowId AND grp.[IsActive] = 1;

UPDATE [NavigationRoutes]
SET [IsActive] = 0, [ModifiedAt] = @now, [ModifiedBy] = N'system'
WHERE [WindowId] IN (N'win-pos', N'win-commercial', N'win-purchasing', N'win-stock', N'win-config');
");
        }
    }
}
