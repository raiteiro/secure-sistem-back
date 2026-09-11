using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndRoleNavigationRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleNavigationRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    NavigationRouteId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleNavigationRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleNavigationRoutes_NavigationRoutes_NavigationRouteId",
                        column: x => x.NavigationRouteId,
                        principalTable: "NavigationRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleNavigationRoutes_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserNavigationRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NavigationRouteId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNavigationRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNavigationRoutes_NavigationRoutes_NavigationRouteId",
                        column: x => x.NavigationRouteId,
                        principalTable: "NavigationRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserNavigationRoutes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleNavigationRoutes_NavigationRouteId",
                table: "RoleNavigationRoutes",
                column: "NavigationRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleNavigationRoutes_RoleId_NavigationRouteId",
                table: "RoleNavigationRoutes",
                columns: new[] { "RoleId", "NavigationRouteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNavigationRoutes_NavigationRouteId",
                table: "UserNavigationRoutes",
                column: "NavigationRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNavigationRoutes_UserId_NavigationRouteId",
                table: "UserNavigationRoutes",
                columns: new[] { "UserId", "NavigationRouteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleNavigationRoutes");

            migrationBuilder.DropTable(
                name: "UserNavigationRoutes");
        }
    }
}
