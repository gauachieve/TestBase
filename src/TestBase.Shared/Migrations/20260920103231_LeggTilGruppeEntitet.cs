using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// HÅNDSKREVET REKKEFØLGE (2026-09-20, se docs/beslutningslogg.md
    /// "Invitasjons- og gruppesystem") — den autogenererte migrasjonen satte
    /// DropColumn("Gruppenavn") FØRST, som ville ha ødelagt dataene før de
    /// kunne kopieres over til de nye Gruppe-radene. Rekkefølgen her er:
    /// opprett grupper-tabellen → legg til GruppeId → KOPIER data (én ny
    /// Gruppe-rad per unike (Gruppenavn, BehandlerId)-par, QrToken generert
    /// med MySQL sin UUID()) → koble Pasient.GruppeId til riktig gruppe →
    /// FØRST DA droppes Gruppenavn-kolonnen.
    /// </remarks>
    public partial class LeggTilGruppeEntitet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grupper",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Navn = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ErArkivert = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArkivertUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    QrToken = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupper", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "gruppe_test_tilordninger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GruppeId = table.Column<long>(type: "bigint", nullable: false),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gruppe_test_tilordninger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "GruppeId",
                table: "pasienter",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pasienter_GruppeId",
                table: "pasienter",
                column: "GruppeId");

            migrationBuilder.CreateIndex(
                name: "IX_gruppe_test_tilordninger_GruppeId_TestId",
                table: "gruppe_test_tilordninger",
                columns: new[] { "GruppeId", "TestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grupper_BehandlerId",
                table: "grupper",
                column: "BehandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_grupper_QrToken",
                table: "grupper",
                column: "QrToken",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_pasienter_grupper_GruppeId",
                table: "pasienter",
                column: "GruppeId",
                principalTable: "grupper",
                principalColumn: "Id");

            // --- Datamigrasjon: én ny Gruppe-rad per unike (Gruppenavn, BehandlerId)
            // -par som faktisk er i bruk, deretter kobling av Pasient.GruppeId. ---
            migrationBuilder.Sql(@"
                INSERT INTO grupper (Navn, BehandlerId, OpprettetUtc, ErArkivert, QrToken)
                SELECT unike.Gruppenavn, unike.BehandlerId, UTC_TIMESTAMP(6), 0, REPLACE(UUID(), '-', '')
                FROM (
                    SELECT DISTINCT Gruppenavn, BehandlerId
                    FROM pasienter
                    WHERE Gruppenavn IS NOT NULL AND Gruppenavn <> ''
                ) AS unike;
            ");

            migrationBuilder.Sql(@"
                UPDATE pasienter p
                JOIN grupper g ON g.Navn = p.Gruppenavn AND g.BehandlerId = p.BehandlerId
                SET p.GruppeId = g.Id
                WHERE p.Gruppenavn IS NOT NULL AND p.Gruppenavn <> '';
            ");

            migrationBuilder.DropColumn(
                name: "Gruppenavn",
                table: "pasienter");
        }

        /// <inheritdoc />
        /// <remarks>
        /// Gjenoppretter KUN skjemaet, ikke dataene — en gruppes navn kan i
        /// prinsippet være endret etter migrering fremover, så en reversering
        /// ville uansett ikke nødvendigvis gjenspeile hva som opprinnelig sto
        /// i Gruppenavn. Samme forbehold som andre datamigreringer i prosjektet.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gruppenavn",
                table: "pasienter",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                UPDATE pasienter p
                JOIN grupper g ON g.Id = p.GruppeId
                SET p.Gruppenavn = g.Navn;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_pasienter_grupper_GruppeId",
                table: "pasienter");

            migrationBuilder.DropTable(
                name: "gruppe_test_tilordninger");

            migrationBuilder.DropTable(
                name: "grupper");

            migrationBuilder.DropIndex(
                name: "IX_pasienter_GruppeId",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "GruppeId",
                table: "pasienter");
        }
    }
}
