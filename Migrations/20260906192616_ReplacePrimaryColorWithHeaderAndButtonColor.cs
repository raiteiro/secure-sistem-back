using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePrimaryColorWithHeaderAndButtonColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PrimaryColor",
                table: "Companies",
                newName: "HeaderColor");

            migrationBuilder.AddColumn<string>(
                name: "ButtonColor",
                table: "Companies",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ButtonColor",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "HeaderColor",
                table: "Companies",
                newName: "PrimaryColor");
        }
    }
}
