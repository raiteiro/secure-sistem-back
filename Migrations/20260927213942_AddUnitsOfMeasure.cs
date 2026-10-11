using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <summary>
    /// Replaces the free-text Product.Unit with a FK to a new global UnitsOfMeasure catalog.
    /// Order matters (the scaffolded version dropped Unit first, losing the data):
    ///   1. create and seed the catalog;
    ///   2. add Products.UnitId (nullable) and map every existing Unit text onto it
    ///      (case/accent/space-insensitive), falling back to "Pieza";
    ///   3. write an audit report to [UnitMigrationReport]: products that fell back to
    ///      "Pieza" because their text wasn't recognized, plus existing quantities (stock,
    ///      min stock, recipe lines, combo lines) with more decimals than their new unit
    ///      allows — never rounded automatically;
    ///   4. make UnitId required, add the FK, and only then drop Unit;
    ///   5. add the "Unidades de medida" screen under "Administración" for every company.
    /// </summary>
    public partial class AddUnitsOfMeasure : Migration
    {
        // Every recognized spelling, compared after trimming, removing spaces, and with a
        // case- and accent-insensitive collation. Anything else (including null/empty) → Pieza.
        private const string MapAndReportSql = @"
DECLARE @now datetime2 = CONVERT(datetime2, SYSDATETIMEOFFSET() AT TIME ZONE 'Central Standard Time (Mexico)');

DECLARE @map TABLE (ProductId int PRIMARY KEY, OriginalUnit nvarchar(20) NULL, UnitName nvarchar(50) NULL);

INSERT INTO @map (ProductId, OriginalUnit, UnitName)
SELECT p.[Id], p.[Unit],
    CASE
        WHEN x.n IN (N'pz', N'pza', N'pieza', N'piezas', N'unidad', N'unidades', N'u') THEN N'Pieza'
        WHEN x.n IN (N'kg', N'kilo', N'kilos', N'kilogramo', N'kilogramos') THEN N'Kilogramo'
        WHEN x.n IN (N'g', N'gr', N'gramo', N'gramos') THEN N'Gramo'
        WHEN x.n IN (N'l', N'lt', N'litro', N'litros') THEN N'Litro'
        WHEN x.n IN (N'ml', N'mililitro', N'mililitros') THEN N'Mililitro'
        WHEN x.n IN (N'm', N'mt', N'metro', N'metros') THEN N'Metro'
        WHEN x.n IN (N'cm', N'centimetro', N'centimetros') THEN N'Centímetro'
        WHEN x.n IN (N'caja', N'cajas') THEN N'Caja'
        WHEN x.n IN (N'paquete', N'paquetes', N'paq') THEN N'Paquete'
        ELSE NULL
    END
FROM [Products] p
CROSS APPLY (SELECT REPLACE(LTRIM(RTRIM(ISNULL(p.[Unit], N''))), N' ', N'') COLLATE Latin1_General_CI_AI AS n) x;

UPDATE p
SET p.[UnitId] = u.[Id]
FROM [Products] p
JOIN @map m ON m.ProductId = p.[Id]
JOIN [UnitsOfMeasure] u ON u.[Name] = ISNULL(m.UnitName, N'Pieza');

INSERT INTO [UnitMigrationReport] ([ProductId], [ProductName], [CompanyId], [OriginalUnit], [AssignedUnit], [Issue], [Detail], [CreatedAt])
SELECT p.[Id], p.[Name], p.[CompanyId], m.OriginalUnit, N'Pieza', N'UnrecognizedUnit',
       CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(m.OriginalUnit, N''))), N'') IS NULL
            THEN N'Sin unidad capturada: se asignó Pieza.'
            ELSE N'Texto de unidad no reconocido: se asignó Pieza.' END,
       @now
FROM @map m
JOIN [Products] p ON p.[Id] = m.ProductId
WHERE m.UnitName IS NULL;

INSERT INTO [UnitMigrationReport] ([ProductId], [ProductName], [CompanyId], [OriginalUnit], [AssignedUnit], [Issue], [Detail], [CreatedAt])
SELECT p.[Id], p.[Name], p.[CompanyId], m.OriginalUnit, u.[Name], N'InventoryDecimals',
       N'Stock de ' + CONVERT(nvarchar(40), i.[Quantity]) + N' en el almacén ' + CONVERT(nvarchar(20), i.[WarehouseId])
           + N' tiene más de ' + CONVERT(nvarchar(5), u.[DecimalPlaces]) + N' decimales.',
       @now
FROM [Inventories] i
JOIN [Products] p ON p.[Id] = i.[ProductId]
JOIN [UnitsOfMeasure] u ON u.[Id] = p.[UnitId]
JOIN @map m ON m.ProductId = p.[Id]
WHERE i.[Quantity] <> ROUND(i.[Quantity], u.[DecimalPlaces]);

INSERT INTO [UnitMigrationReport] ([ProductId], [ProductName], [CompanyId], [OriginalUnit], [AssignedUnit], [Issue], [Detail], [CreatedAt])
SELECT p.[Id], p.[Name], p.[CompanyId], m.OriginalUnit, u.[Name], N'MinStockDecimals',
       N'Stock mínimo de ' + CONVERT(nvarchar(40), i.[MinStock]) + N' en el almacén ' + CONVERT(nvarchar(20), i.[WarehouseId])
           + N' tiene más de ' + CONVERT(nvarchar(5), u.[DecimalPlaces]) + N' decimales.',
       @now
FROM [Inventories] i
JOIN [Products] p ON p.[Id] = i.[ProductId]
JOIN [UnitsOfMeasure] u ON u.[Id] = p.[UnitId]
JOIN @map m ON m.ProductId = p.[Id]
WHERE i.[MinStock] IS NOT NULL AND i.[MinStock] <> ROUND(i.[MinStock], u.[DecimalPlaces]);

INSERT INTO [UnitMigrationReport] ([ProductId], [ProductName], [CompanyId], [OriginalUnit], [AssignedUnit], [Issue], [Detail], [CreatedAt])
SELECT p.[Id], p.[Name], p.[CompanyId], m.OriginalUnit, u.[Name], N'RecipeDecimals',
       N'Receta del producto ' + CONVERT(nvarchar(20), ri.[ProductId]) + N' usa ' + CONVERT(nvarchar(40), ri.[Quantity])
           + N', con más de ' + CONVERT(nvarchar(5), u.[DecimalPlaces]) + N' decimales.',
       @now
FROM [ProductRecipeItems] ri
JOIN [Products] p ON p.[Id] = ri.[SupplyProductId]
JOIN [UnitsOfMeasure] u ON u.[Id] = p.[UnitId]
JOIN @map m ON m.ProductId = p.[Id]
WHERE ri.[IsActive] = 1 AND ri.[Quantity] <> ROUND(ri.[Quantity], u.[DecimalPlaces]);

INSERT INTO [UnitMigrationReport] ([ProductId], [ProductName], [CompanyId], [OriginalUnit], [AssignedUnit], [Issue], [Detail], [CreatedAt])
SELECT p.[Id], p.[Name], p.[CompanyId], m.OriginalUnit, u.[Name], N'ComboDecimals',
       N'Combo ' + CONVERT(nvarchar(20), ci.[ComboProductId]) + N' usa ' + CONVERT(nvarchar(40), ci.[Quantity])
           + N', con más de ' + CONVERT(nvarchar(5), u.[DecimalPlaces]) + N' decimales.',
       @now
FROM [ProductComboItems] ci
JOIN [Products] p ON p.[Id] = ci.[ComponentProductId]
JOIN [UnitsOfMeasure] u ON u.[Id] = p.[UnitId]
JOIN @map m ON m.ProductId = p.[Id]
WHERE ci.[IsActive] = 1 AND ci.[Quantity] <> ROUND(ci.[Quantity], u.[DecimalPlaces]);
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitsOfMeasure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitsOfMeasure", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_Abbreviation",
                table: "UnitsOfMeasure",
                column: "Abbreviation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitsOfMeasure_Name",
                table: "UnitsOfMeasure",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(@"
DECLARE @now datetime2 = CONVERT(datetime2, SYSDATETIMEOFFSET() AT TIME ZONE 'Central Standard Time (Mexico)');

INSERT INTO [UnitsOfMeasure] ([Name], [Abbreviation], [DecimalPlaces], [IsActive], [CreatedAt], [CreatedBy])
SELECT v.[Name], v.[Abbreviation], v.[DecimalPlaces], 1, @now, N'system'
FROM (VALUES
    (N'Pieza', N'pz', 0),
    (N'Caja', N'caja', 0),
    (N'Paquete', N'paq', 0),
    (N'Kilogramo', N'kg', 3),
    (N'Gramo', N'g', 0),
    (N'Litro', N'L', 3),
    (N'Mililitro', N'ml', 0),
    (N'Metro', N'm', 2),
    (N'Centímetro', N'cm', 0)
) AS v([Name], [Abbreviation], [DecimalPlaces])
WHERE NOT EXISTS (SELECT 1 FROM [UnitsOfMeasure] u WHERE u.[Name] = v.[Name]);

-- Audit report of the Unit → UnitId migration, for manual review. Not part of the EF
-- model: query it directly, and drop it once reviewed (see TECH_DEBT.md).
IF OBJECT_ID(N'[UnitMigrationReport]') IS NULL
    CREATE TABLE [UnitMigrationReport] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProductId] int NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [CompanyId] int NOT NULL,
        [OriginalUnit] nvarchar(20) NULL,
        [AssignedUnit] nvarchar(50) NOT NULL,
        [Issue] nvarchar(30) NOT NULL,
        [Detail] nvarchar(500) NOT NULL,
        [CreatedAt] datetime2 NOT NULL
    );
");

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Products",
                type: "int",
                nullable: true);

            // Wrapped in EXEC so the idempotent script still compiles when re-run after
            // Products.Unit is gone (SQL Server compiles a whole batch, even skipped blocks).
            migrationBuilder.Sql("EXEC(N'" + MapAndReportSql.Replace("'", "''") + "');");

            migrationBuilder.AlterColumn<int>(
                name: "UnitId",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                table: "Products",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UnitsOfMeasure_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "UnitsOfMeasure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Products");

            // "Unidades de medida" screen under each company's "Administración" group, same
            // pattern as the other admin screens: not a default for new roles, and backfilled
            // only to the roles that already manage users (the company's admin roles).
            // CompaniesController.Create adds it for new companies.
            migrationBuilder.Sql(@"
DECLARE @now datetime2 = CONVERT(datetime2, SYSDATETIMEOFFSET() AT TIME ZONE 'Central Standard Time (Mexico)');

INSERT INTO [NavigationRoutes] ([ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
SELECT admin.[Id], N'Unidades de medida', N'/admin/unidades-medida', N'fas fa-ruler', N'win-units', 1, 5, admin.[CompanyId], 1, 0, @now, N'system'
FROM [NavigationRoutes] admin
WHERE admin.[WindowId] = N'win-admin' AND admin.[IsActive] = 1
  AND NOT EXISTS (SELECT 1 FROM [NavigationRoutes] r WHERE r.[CompanyId] = admin.[CompanyId] AND r.[WindowId] = N'win-units');

INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
SELECT rnr.[RoleId], units.[Id], 1, @now, N'system'
FROM [RoleNavigationRoutes] rnr
JOIN [NavigationRoutes] usersRoute ON usersRoute.[Id] = rnr.[NavigationRouteId] AND usersRoute.[WindowId] = N'win-users'
JOIN [Roles] r ON r.[Id] = rnr.[RoleId] AND r.[IsActive] = 1
JOIN [NavigationRoutes] units ON units.[CompanyId] = usersRoute.[CompanyId] AND units.[WindowId] = N'win-units'
WHERE rnr.[IsActive] = 1
  AND NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] x WHERE x.[RoleId] = rnr.[RoleId] AND x.[NavigationRouteId] = units.[Id]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE rnr FROM [RoleNavigationRoutes] rnr
JOIN [NavigationRoutes] r ON r.[Id] = rnr.[NavigationRouteId]
WHERE r.[WindowId] = N'win-units';

DELETE FROM [NavigationRoutes] WHERE [WindowId] = N'win-units';
");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("EXEC(N'UPDATE p SET p.[Unit] = u.[Name] FROM [Products] p JOIN [UnitsOfMeasure] u ON u.[Id] = p.[UnitId];');");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_UnitsOfMeasure_UnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "UnitsOfMeasure");

            migrationBuilder.Sql("IF OBJECT_ID(N'[UnitMigrationReport]') IS NOT NULL DROP TABLE [UnitMigrationReport];");
        }
    }
}
