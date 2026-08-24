using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Fase4PasientOgTestmotor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "pasienter",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BiologiskKjonnVedFodsel",
                table: "pasienter",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BrukeravtaleGodkjentUtc",
                table: "pasienter",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BrukeravtaleGodkjentVersjon",
                table: "pasienter",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GodtarLagringAvData",
                table: "pasienter",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GodtarMuligVippsBetaling",
                table: "pasienter",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kjonnsidentitet",
                table: "pasienter",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "KjonnsidentitetSpesifisert",
                table: "pasienter",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegistrertUtc",
                table: "pasienter",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "test_ledd",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TestSideId = table.Column<long>(type: "bigint", nullable: false),
                    Rekkefolge = table.Column<int>(type: "int", nullable: false),
                    Sporsmalstekst = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instruksjon = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Svartype = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Svaralternativer = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_ledd", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "test_sider",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    Rekkefolge = table.Column<int>(type: "int", nullable: false),
                    Navn = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instruksjon = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_sider", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "test_svar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TestTildelingId = table.Column<long>(type: "bigint", nullable: false),
                    TestLeddId = table.Column<long>(type: "bigint", nullable: false),
                    SvarVerdi = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BesvartUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_svar", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "test_tildelinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    PasientId = table.Column<long>(type: "bigint", nullable: false),
                    TildeltAvBehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    TildeltUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Frist = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    VarighetMinutter = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    FullfortUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_tildelinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tester",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Navn = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Beskrivelse = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Belonningstekst = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErAktiv = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tester", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_test_ledd_TestSideId",
                table: "test_ledd",
                column: "TestSideId");

            migrationBuilder.CreateIndex(
                name: "IX_test_sider_TestId",
                table: "test_sider",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_test_svar_TestTildelingId_TestLeddId",
                table: "test_svar",
                columns: new[] { "TestTildelingId", "TestLeddId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_tildelinger_PasientId",
                table: "test_tildelinger",
                column: "PasientId");

            migrationBuilder.CreateIndex(
                name: "IX_test_tildelinger_TestId",
                table: "test_tildelinger",
                column: "TestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_ledd");

            migrationBuilder.DropTable(
                name: "test_sider");

            migrationBuilder.DropTable(
                name: "test_svar");

            migrationBuilder.DropTable(
                name: "test_tildelinger");

            migrationBuilder.DropTable(
                name: "tester");

            migrationBuilder.DropColumn(
                name: "Adresse",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "BiologiskKjonnVedFodsel",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "BrukeravtaleGodkjentUtc",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "BrukeravtaleGodkjentVersjon",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "GodtarLagringAvData",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "GodtarMuligVippsBetaling",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "Kjonnsidentitet",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "KjonnsidentitetSpesifisert",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "RegistrertUtc",
                table: "pasienter");
        }
    }
}
