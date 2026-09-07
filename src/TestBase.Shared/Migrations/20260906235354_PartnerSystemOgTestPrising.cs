using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class PartnerSystemOgTestPrising : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinstePartnerAndelKr",
                table: "tester",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinstePrisKr",
                table: "tester",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StorstePrisKr",
                table: "tester",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TypiskBehandlerHonorarKr",
                table: "tester",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ErPartnerAdministrator",
                table: "behandlere",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HarEgetAbonnement",
                table: "behandlere",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PartnerId",
                table: "behandlere",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErSuperadmin",
                table: "administratorer",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "partner_test_andeler",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartnerId = table.Column<long>(type: "bigint", nullable: false),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    AndelKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SistEndretAvBehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    SistEndretUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_test_andeler", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "partner_test_tilganger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PartnerId = table.Column<long>(type: "bigint", nullable: false),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    GittAvAdministratorId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_test_tilganger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "partnere",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Navn = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KontaktpersonNavn = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KontaktEpost = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KontaktMobilNr = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HarAktivtAbonnement = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OpprettetAvAdministratorId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ErArkivert = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArkivertUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partnere", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pengebevegelser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BelopKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TestTildelingId = table.Column<long>(type: "bigint", nullable: true),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: true),
                    PartnerId = table.Column<long>(type: "bigint", nullable: true),
                    Beskrivelse = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pengebevegelser", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "test_tildeling_betalinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TestTildelingId = table.Column<long>(type: "bigint", nullable: false),
                    PasientTotalprisKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BehandlerHonorarKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PlattformAndelKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PartnerAndelKr = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PartnerId = table.Column<long>(type: "bigint", nullable: true),
                    DekketAvAbonnement = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metode = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BetalingsleverandorReferanse = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    BetaltUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_tildeling_betalinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_behandlere_PartnerId",
                table: "behandlere",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_partner_test_andeler_PartnerId_TestId",
                table: "partner_test_andeler",
                columns: new[] { "PartnerId", "TestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_partner_test_tilganger_PartnerId_TestId",
                table: "partner_test_tilganger",
                columns: new[] { "PartnerId", "TestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pengebevegelser_BehandlerId",
                table: "pengebevegelser",
                column: "BehandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_pengebevegelser_PartnerId",
                table: "pengebevegelser",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_pengebevegelser_TestTildelingId",
                table: "pengebevegelser",
                column: "TestTildelingId");

            migrationBuilder.CreateIndex(
                name: "IX_test_tildeling_betalinger_TestTildelingId",
                table: "test_tildeling_betalinger",
                column: "TestTildelingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner_test_andeler");

            migrationBuilder.DropTable(
                name: "partner_test_tilganger");

            migrationBuilder.DropTable(
                name: "partnere");

            migrationBuilder.DropTable(
                name: "pengebevegelser");

            migrationBuilder.DropTable(
                name: "test_tildeling_betalinger");

            migrationBuilder.DropIndex(
                name: "IX_behandlere_PartnerId",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "MinstePartnerAndelKr",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "MinstePrisKr",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "StorstePrisKr",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "TypiskBehandlerHonorarKr",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "ErPartnerAdministrator",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "HarEgetAbonnement",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "ErSuperadmin",
                table: "administratorer");
        }
    }
}
