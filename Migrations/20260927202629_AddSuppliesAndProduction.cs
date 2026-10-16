using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliesAndProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManufactured",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Product");

            migrationBuilder.AddColumn<int>(
                name: "ProductionOrderId",
                table: "InventoryMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FolioNumber = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductRecipeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SupplyProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRecipeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRecipeItems_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRecipeItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRecipeItems_Products_SupplyProductId",
                        column: x => x.SupplyProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    SupplyProductId = table.Column<int>(type: "int", nullable: false),
                    QuantityPerUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrderItems_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrderItems_Products_SupplyProductId",
                        column: x => x.SupplyProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductionOrderId",
                table: "InventoryMovements",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderItems_ProductionOrderId",
                table: "ProductionOrderItems",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrderItems_SupplyProductId",
                table: "ProductionOrderItems",
                column: "SupplyProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_CompanyId_FolioNumber",
                table: "ProductionOrders",
                columns: new[] { "CompanyId", "FolioNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductId",
                table: "ProductionOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_UserId",
                table: "ProductionOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_WarehouseId",
                table: "ProductionOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecipeItems_CompanyId",
                table: "ProductRecipeItems",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecipeItems_ProductId_SupplyProductId",
                table: "ProductRecipeItems",
                columns: new[] { "ProductId", "SupplyProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductRecipeItems_SupplyProductId",
                table: "ProductRecipeItems",
                column: "SupplyProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_ProductionOrders_ProductionOrderId",
                table: "InventoryMovements",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Action permissions for the production features of the "Almacenes" screen. Like
            // the original catalog (AddPermissions), they start IsDefaultForNewRoles = 1 and are
            // granted to every active role, so nothing is hidden until a company revokes them.
            // Frontend-only gating: no endpoint checks them server-side (see TECH_DEBT.md).
            migrationBuilder.Sql(@"
DECLARE @now datetime2 = DATEADD(HOUR, -6, SYSUTCDATETIME());

INSERT INTO [Permissions] ([Key], [Name], [Description], [WindowId], [IsActive], [IsDefaultForNewRoles], [CreatedAt], [CreatedBy])
SELECT v.[Key], v.[Name], NULL, N'win-warehouses', 1, 1, @now, N'system'
FROM (VALUES
    (N'WAREHOUSES.PRODUCE', N'Fabricar productos con insumos'),
    (N'WAREHOUSES.CREATE_MANUFACTURED', N'Dar de alta un producto fabricado'),
    (N'WAREHOUSES.EDIT_RECIPE', N'Editar receta de producto fabricado'),
    (N'WAREHOUSES.DISASSEMBLE', N'Separar producto en sus insumos')
) AS v([Key], [Name])
WHERE NOT EXISTS (SELECT 1 FROM [Permissions] p WHERE p.[Key] = v.[Key]);

INSERT INTO [RolePermissions] ([RoleId], [PermissionId], [IsActive], [CreatedAt], [CreatedBy])
SELECT r.[Id], p.[Id], 1, @now, N'system'
FROM [Roles] r
CROSS JOIN [Permissions] p
WHERE r.[IsActive] = 1
  AND p.[Key] IN (N'WAREHOUSES.PRODUCE', N'WAREHOUSES.CREATE_MANUFACTURED', N'WAREHOUSES.EDIT_RECIPE', N'WAREHOUSES.DISASSEMBLE')
  AND NOT EXISTS (SELECT 1 FROM [RolePermissions] rp WHERE rp.[RoleId] = r.[Id] AND rp.[PermissionId] = p.[Id]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE rp FROM [RolePermissions] rp
JOIN [Permissions] p ON p.[Id] = rp.[PermissionId]
WHERE p.[Key] IN (N'WAREHOUSES.PRODUCE', N'WAREHOUSES.CREATE_MANUFACTURED', N'WAREHOUSES.EDIT_RECIPE', N'WAREHOUSES.DISASSEMBLE');

DELETE FROM [Permissions]
WHERE [Key] IN (N'WAREHOUSES.PRODUCE', N'WAREHOUSES.CREATE_MANUFACTURED', N'WAREHOUSES.EDIT_RECIPE', N'WAREHOUSES.DISASSEMBLE');
");
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_ProductionOrders_ProductionOrderId",
                table: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "ProductionOrderItems");

            migrationBuilder.DropTable(
                name: "ProductRecipeItems");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_ProductionOrderId",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "IsManufactured",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                table: "InventoryMovements");
        }
    }
}
