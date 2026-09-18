using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WindowId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultForNewRoles = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            // Seed the action-permission catalog (see permissions.md at the repo root for the
            // full window/action tree this comes from). All start IsDefaultForNewRoles=true —
            // the rollout is permissive by default (mirrors NavigationRoute.IsDefaultForNewRoles),
            // so nothing breaks for existing workflows; companies selectively revoke per role
            // afterward via POST /api/roles/{id}/permissions.
            var seedDate = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Unspecified);
            const string seedBy = "system";

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Key", "Name", "Description", "WindowId", "IsActive", "IsDefaultForNewRoles", "CreatedAt", "CreatedBy" },
                values: new object[,]
                {
                    { "USERS.CREATE", "Crear usuario", null, "win-users", true, true, seedDate, seedBy },
                    { "USERS.EDIT", "Editar usuario", null, "win-users", true, true, seedDate, seedBy },
                    { "USERS.DEACTIVATE", "Desactivar usuario", null, "win-users", true, true, seedDate, seedBy },
                    { "USERS.RESET_PASSWORD", "Restablecer contraseña", null, "win-users", true, true, seedDate, seedBy },
                    { "USERS.REASSIGN_COMPANY", "Cambiar de empresa", null, "win-users", true, true, seedDate, seedBy },

                    { "NAV_ROUTES.CREATE", "Crear ruta", null, "win-routes", true, true, seedDate, seedBy },
                    { "NAV_ROUTES.EDIT", "Editar ruta", null, "win-routes", true, true, seedDate, seedBy },
                    { "NAV_ROUTES.DEACTIVATE", "Desactivar ruta", null, "win-routes", true, true, seedDate, seedBy },

                    { "ROLES.CREATE", "Crear rol", null, "win-roles", true, true, seedDate, seedBy },
                    { "ROLES.EDIT", "Editar rol", null, "win-roles", true, true, seedDate, seedBy },
                    { "ROLES.DEACTIVATE", "Desactivar rol", null, "win-roles", true, true, seedDate, seedBy },
                    { "ROLES.ASSIGN_ROUTES", "Asignar ventanas al rol", null, "win-roles", true, true, seedDate, seedBy },

                    { "COMPANIES.CREATE", "Crear empresa", null, "win-company", true, true, seedDate, seedBy },
                    { "COMPANIES.EDIT", "Editar empresa", null, "win-company", true, true, seedDate, seedBy },
                    { "COMPANIES.DEACTIVATE", "Desactivar empresa", null, "win-company", true, true, seedDate, seedBy },
                    { "COMPANIES.UPLOAD_LOGO", "Subir/cambiar logo", null, "win-company", true, true, seedDate, seedBy },

                    { "BRANCHES.CREATE", "Crear sucursal", null, "win-branches", true, true, seedDate, seedBy },
                    { "BRANCHES.EDIT", "Editar sucursal", null, "win-branches", true, true, seedDate, seedBy },
                    { "BRANCHES.DEACTIVATE", "Desactivar sucursal", null, "win-branches", true, true, seedDate, seedBy },

                    { "WAREHOUSES.CREATE", "Crear almacén", null, "win-warehouses", true, true, seedDate, seedBy },
                    { "WAREHOUSES.EDIT", "Editar almacén", null, "win-warehouses", true, true, seedDate, seedBy },
                    { "WAREHOUSES.DEACTIVATE", "Desactivar almacén", null, "win-warehouses", true, true, seedDate, seedBy },
                    { "WAREHOUSES.ADD_PRODUCT", "Agregar producto al almacén", null, "win-warehouses", true, true, seedDate, seedBy },
                    { "WAREHOUSES.EDIT_MIN_STOCK", "Editar stock mínimo", null, "win-warehouses", true, true, seedDate, seedBy },
                    { "WAREHOUSES.REGISTER_MOVEMENT", "Registrar movimiento de inventario", null, "win-warehouses", true, true, seedDate, seedBy },

                    { "TAX_RATES.CREATE", "Crear impuesto", null, "win-taxrates", true, true, seedDate, seedBy },
                    { "TAX_RATES.EDIT", "Editar impuesto", null, "win-taxrates", true, true, seedDate, seedBy },
                    { "TAX_RATES.DEACTIVATE", "Desactivar impuesto", null, "win-taxrates", true, true, seedDate, seedBy },

                    { "CATEGORIES.CREATE", "Crear categoría", null, "win-categories", true, true, seedDate, seedBy },
                    { "CATEGORIES.EDIT", "Editar categoría", null, "win-categories", true, true, seedDate, seedBy },
                    { "CATEGORIES.DEACTIVATE", "Desactivar categoría", null, "win-categories", true, true, seedDate, seedBy },

                    { "PRODUCTS.CREATE", "Crear producto", null, "win-products", true, true, seedDate, seedBy },
                    { "PRODUCTS.EDIT", "Editar producto", null, "win-products", true, true, seedDate, seedBy },
                    { "PRODUCTS.DEACTIVATE", "Desactivar producto", null, "win-products", true, true, seedDate, seedBy },

                    { "CUSTOMERS.CREATE", "Crear cliente", null, "win-customers", true, true, seedDate, seedBy },
                    { "CUSTOMERS.EDIT", "Editar cliente", null, "win-customers", true, true, seedDate, seedBy },
                    { "CUSTOMERS.DEACTIVATE", "Desactivar cliente", null, "win-customers", true, true, seedDate, seedBy },

                    { "CASH_REGISTERS.CREATE", "Crear caja", null, "win-cashregisters", true, true, seedDate, seedBy },
                    { "CASH_REGISTERS.EDIT", "Editar caja", null, "win-cashregisters", true, true, seedDate, seedBy },
                    { "CASH_REGISTERS.DEACTIVATE", "Desactivar caja", null, "win-cashregisters", true, true, seedDate, seedBy },

                    { "CASH_SESSIONS.OPEN", "Abrir turno", null, "win-cashsessions", true, true, seedDate, seedBy },
                    { "CASH_SESSIONS.CLOSE", "Cerrar turno", null, "win-cashsessions", true, true, seedDate, seedBy },

                    { "SALES.REGISTER", "Registrar venta", null, "win-sales", true, true, seedDate, seedBy },
                    { "SALES.CANCEL", "Cancelar venta", null, "win-sales", true, true, seedDate, seedBy },
                    { "SALES.SEND_RECEIPT", "Enviar recibo por correo", null, "win-sales", true, true, seedDate, seedBy },

                    { "RETURNS.REGISTER", "Registrar devolución", null, "win-returns", true, true, seedDate, seedBy }
                });

            // Backfill: grant every default permission to every currently active role, in
            // every company — same intent as the manual per-company backfill CLAUDE.md
            // documents for routes, but doable in one shot here since the permission catalog
            // is global rather than duplicated per company.
            migrationBuilder.Sql(@"
                INSERT INTO RolePermissions (RoleId, PermissionId, IsActive, CreatedAt, CreatedBy)
                SELECT r.Id, p.Id, 1, GETDATE(), N'system'
                FROM Roles r
                CROSS JOIN Permissions p
                WHERE r.IsActive = 1 AND p.IsDefaultForNewRoles = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Permissions");
        }
    }
}
