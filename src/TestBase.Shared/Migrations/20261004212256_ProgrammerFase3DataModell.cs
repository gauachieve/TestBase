using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class ProgrammerFase3DataModell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "behandlingsprogrammer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Navn = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Forklaring = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartUkedag = table.Column<int>(type: "int", nullable: false),
                    StartKlokkeslett = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    OpprettetAvBehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ErArkivert = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErDeltMedAlle = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErDeltMedPartner = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    KopiertFraProgramId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behandlingsprogrammer", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "program_deltakelser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProgramId = table.Column<long>(type: "bigint", nullable: false),
                    PasientId = table.Column<long>(type: "bigint", nullable: false),
                    GruppeId = table.Column<long>(type: "bigint", nullable: true),
                    TildeltAvBehandlerId = table.Column<long>(type: "bigint", nullable: true),
                    TildeltAvAdministratorId = table.Column<long>(type: "bigint", nullable: true),
                    ProgramStartUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    NaavaerendeDroppIndeks = table.Column<int>(type: "int", nullable: false),
                    NesteDroppPlanlagtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    PauseUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    MeldtUtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    FullfortUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_deltakelser", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "program_drop_tester",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProgramDropId = table.Column<long>(type: "bigint", nullable: false),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    Rekkefolge = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_drop_tester", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "program_drops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProgramId = table.Column<long>(type: "bigint", nullable: false),
                    Rekkefolge = table.Column<int>(type: "int", nullable: false),
                    DagerEtterForrige = table.Column<int>(type: "int", nullable: false),
                    FraKlokkeslett = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    TilKlokkeslett = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    UnngaaNatt = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_drops", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "program_tildelinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProgramDeltakelseId = table.Column<long>(type: "bigint", nullable: false),
                    ProgramDropId = table.Column<long>(type: "bigint", nullable: false),
                    TestTildelingId = table.Column<long>(type: "bigint", nullable: false),
                    RekkefolgeIDrop = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_tildelinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_behandlingsprogrammer_OpprettetAvBehandlerId",
                table: "behandlingsprogrammer",
                column: "OpprettetAvBehandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_program_deltakelser_NesteDroppPlanlagtUtc",
                table: "program_deltakelser",
                column: "NesteDroppPlanlagtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_program_deltakelser_PasientId",
                table: "program_deltakelser",
                column: "PasientId");

            migrationBuilder.CreateIndex(
                name: "IX_program_deltakelser_ProgramId",
                table: "program_deltakelser",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_drop_tester_ProgramDropId",
                table: "program_drop_tester",
                column: "ProgramDropId");

            migrationBuilder.CreateIndex(
                name: "IX_program_drops_ProgramId",
                table: "program_drops",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_tildelinger_ProgramDeltakelseId_ProgramDropId",
                table: "program_tildelinger",
                columns: new[] { "ProgramDeltakelseId", "ProgramDropId" });

            migrationBuilder.CreateIndex(
                name: "IX_program_tildelinger_TestTildelingId",
                table: "program_tildelinger",
                column: "TestTildelingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "behandlingsprogrammer");

            migrationBuilder.DropTable(
                name: "program_deltakelser");

            migrationBuilder.DropTable(
                name: "program_drop_tester");

            migrationBuilder.DropTable(
                name: "program_drops");

            migrationBuilder.DropTable(
                name: "program_tildelinger");
        }
    }
}
