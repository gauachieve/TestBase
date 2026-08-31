using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Fase6RapportGodkjenningMeldingerOgPaaminnelse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RapportGodkjentUtc",
                table: "test_tildelinger",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RapportSynligForPasient",
                table: "test_tildelinger",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnskerDagligPaaminnelse",
                table: "behandlere",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaaminnelseKanal",
                table: "behandlere",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Begge")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SistPaaminnetUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "behandler_meldinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    TestTildelingId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LestUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behandler_meldinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_behandler_meldinger_BehandlerId_LestUtc",
                table: "behandler_meldinger",
                columns: new[] { "BehandlerId", "LestUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_behandler_meldinger_TestTildelingId",
                table: "behandler_meldinger",
                column: "TestTildelingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "behandler_meldinger");

            migrationBuilder.DropColumn(
                name: "RapportGodkjentUtc",
                table: "test_tildelinger");

            migrationBuilder.DropColumn(
                name: "RapportSynligForPasient",
                table: "test_tildelinger");

            migrationBuilder.DropColumn(
                name: "OnskerDagligPaaminnelse",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "PaaminnelseKanal",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "SistPaaminnetUtc",
                table: "behandlere");
        }
    }
}
