using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilUtbetalingsBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "utbetalings_batcher",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Aar = table.Column<int>(type: "int", nullable: false),
                    Maned = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GenerertUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    GodkjentAvAdministratorId = table.Column<long>(type: "bigint", nullable: true),
                    GodkjentUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    TotalBelopKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utbetalings_batcher", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "utbetalings_linje_pengebevegelser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtbetalingsLinjeId = table.Column<long>(type: "bigint", nullable: false),
                    PengebevegelseId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utbetalings_linje_pengebevegelser", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "utbetalings_linjer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtbetalingsBatchId = table.Column<long>(type: "bigint", nullable: false),
                    MottakerType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: true),
                    PartnerId = table.Column<long>(type: "bigint", nullable: true),
                    BelopKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AntallUnderliggendeTransaksjoner = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StripeTransferId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SisteFeilmelding = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OverfortUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AntallForsok = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utbetalings_linjer", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_batcher_Aar_Maned",
                table: "utbetalings_batcher",
                columns: new[] { "Aar", "Maned" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_linje_pengebevegelser_PengebevegelseId",
                table: "utbetalings_linje_pengebevegelser",
                column: "PengebevegelseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_linje_pengebevegelser_UtbetalingsLinjeId",
                table: "utbetalings_linje_pengebevegelser",
                column: "UtbetalingsLinjeId");

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_linjer_BehandlerId",
                table: "utbetalings_linjer",
                column: "BehandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_linjer_PartnerId",
                table: "utbetalings_linjer",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_linjer_UtbetalingsBatchId",
                table: "utbetalings_linjer",
                column: "UtbetalingsBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "utbetalings_batcher");

            migrationBuilder.DropTable(
                name: "utbetalings_linje_pengebevegelser");

            migrationBuilder.DropTable(
                name: "utbetalings_linjer");
        }
    }
}
