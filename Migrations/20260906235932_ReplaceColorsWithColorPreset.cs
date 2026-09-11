using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceColorsWithColorPreset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ButtonColor",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HeaderColor",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "HeaderTextMode",
                table: "Companies");

            migrationBuilder.AddColumn<string>(
                name: "ColorPreset",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorPreset",
                table: "Companies");

            migrationBuilder.AddColumn<string>(
                name: "ButtonColor",
                table: "Companies",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderColor",
                table: "Companies",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderTextMode",
                table: "Companies",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);
        }
    }
}
