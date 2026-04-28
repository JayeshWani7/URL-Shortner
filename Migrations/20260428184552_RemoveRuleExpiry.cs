using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRuleExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveFromUtc",
                table: "UrlRules");

            migrationBuilder.DropColumn(
                name: "ActiveUntilUtc",
                table: "UrlRules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveFromUtc",
                table: "UrlRules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveUntilUtc",
                table: "UrlRules",
                type: "TEXT",
                nullable: true);
        }
    }
}
