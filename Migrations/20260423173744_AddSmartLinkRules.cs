using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartLinkRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UrlRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShortUrlId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", nullable: true),
                    LanguagePrefix = table.Column<string>(type: "TEXT", nullable: true),
                    ActiveFromUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActiveUntilUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BucketStart = table.Column<int>(type: "INTEGER", nullable: true),
                    BucketEnd = table.Column<int>(type: "INTEGER", nullable: true),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrlRules_Urls_ShortUrlId",
                        column: x => x.ShortUrlId,
                        principalTable: "Urls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UrlRules_ShortUrlId_Priority_Id",
                table: "UrlRules",
                columns: new[] { "ShortUrlId", "Priority", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UrlRules");
        }
    }
}
