using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilTilbakemeldinger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tilbakemeldinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Melding = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BrukerAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SkjermBredde = table.Column<int>(type: "int", nullable: true),
                    SkjermHoyde = table.Column<int>(type: "int", nullable: true),
                    VindaugBredde = table.Column<int>(type: "int", nullable: true),
                    VindaugHoyde = table.Column<int>(type: "int", nullable: true),
                    InnloggetRolle = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InnloggetBrukerId = table.Column<long>(type: "bigint", nullable: true),
                    TekniskFeilInfo = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErKrasjRapport = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScreenshotDataUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notat = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SistOppdatertUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tilbakemeldinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_tilbakemeldinger_ErKrasjRapport",
                table: "tilbakemeldinger",
                column: "ErKrasjRapport");

            migrationBuilder.CreateIndex(
                name: "IX_tilbakemeldinger_OpprettetUtc",
                table: "tilbakemeldinger",
                column: "OpprettetUtc");

            migrationBuilder.CreateIndex(
                name: "IX_tilbakemeldinger_Status",
                table: "tilbakemeldinger",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tilbakemeldinger");
        }
    }
}
