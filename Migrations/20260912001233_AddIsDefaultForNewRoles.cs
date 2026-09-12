using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDefaultForNewRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultForNewRoles",
                table: "NavigationRoutes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefaultForNewRoles",
                table: "NavigationRoutes");
        }
    }
}
