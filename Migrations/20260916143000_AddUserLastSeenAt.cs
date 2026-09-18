using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureSistem.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLastSeenAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Users");
        }
    }
}
