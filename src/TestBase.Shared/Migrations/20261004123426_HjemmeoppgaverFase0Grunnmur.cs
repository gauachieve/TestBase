using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class HjemmeoppgaverFase0Grunnmur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErDeltMedAlle",
                table: "tester",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ErDeltMedPartner",
                table: "tester",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "KopiertFraTestId",
                table: "tester",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OpprettetAvBehandlerId",
                table: "tester",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BildeData",
                table: "test_ledd",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "ErPaakrevd",
                table: "test_ledd",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<long>(
                name: "TestTildelingId",
                table: "behandler_meldinger",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Fritekst",
                table: "behandler_meldinger",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HjemmeoppgaveLikinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HjemmeoppgaveLikinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HjemmeoppgaveLikinger");

            migrationBuilder.DropColumn(
                name: "ErDeltMedAlle",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "ErDeltMedPartner",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "KopiertFraTestId",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "OpprettetAvBehandlerId",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "BildeData",
                table: "test_ledd");

            migrationBuilder.DropColumn(
                name: "ErPaakrevd",
                table: "test_ledd");

            migrationBuilder.DropColumn(
                name: "Fritekst",
                table: "behandler_meldinger");

            migrationBuilder.AlterColumn<long>(
                name: "TestTildelingId",
                table: "behandler_meldinger",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
