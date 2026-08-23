using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Fase3BehandlerOgPasienter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_to_faktor_koder_AdministratorId",
                table: "to_faktor_koder");

            migrationBuilder.RenameColumn(
                name: "AdministratorId",
                table: "to_faktor_koder",
                newName: "PrincipalId");

            migrationBuilder.DropColumn(
                name: "FulltNavn",
                table: "behandlere");

            migrationBuilder.AddColumn<string>(
                name: "Arbeidsadresse",
                table: "behandlere",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrincipalType",
                table: "to_faktor_koder",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<long>(
                name: "InvitertAvAdministratorId",
                table: "behandlere",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BrukeravtaleGodkjentUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BrukeravtaleGodkjentVersjon",
                table: "behandlere",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EpostVerifisertUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Etternavn",
                table: "behandlere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Fornavn",
                table: "behandlere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "HprGodkjent",
                table: "behandlere",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "HprGodkjentAvAdministratorId",
                table: "behandlere",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HprGodkjentUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InvitertAvBehandlerId",
                table: "behandlere",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kontonummer",
                table: "behandlere",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MobilVerifisertUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Personnummer",
                table: "behandlere",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegistrertUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tittel",
                table: "behandlere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<long>(
                name: "OpprettetAvAdministratorId",
                table: "behandler_invitasjoner",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "OpprettetAvBehandlerId",
                table: "behandler_invitasjoner",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "behandler_kontakt_verifiseringer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    Kanal = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KodeHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UtlopUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    BruktUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Forsok = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_behandler_kontakt_verifiseringer", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pasient_invitasjoner",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PasientId = table.Column<long>(type: "bigint", nullable: false),
                    Token = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KontaktMetode = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KontaktVerdi = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UtlopUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    BruktUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    OpprettetAvBehandlerId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pasient_invitasjoner", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pasienter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Personnummer = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MobilNr = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Navn = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Gruppenavn = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ArkivertUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pasienter", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_to_faktor_koder_PrincipalType_PrincipalId",
                table: "to_faktor_koder",
                columns: new[] { "PrincipalType", "PrincipalId" });

            migrationBuilder.CreateIndex(
                name: "IX_behandler_kontakt_verifiseringer_BehandlerId_Kanal",
                table: "behandler_kontakt_verifiseringer",
                columns: new[] { "BehandlerId", "Kanal" });

            migrationBuilder.CreateIndex(
                name: "IX_pasient_invitasjoner_Token",
                table: "pasient_invitasjoner",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pasienter_BehandlerId",
                table: "pasienter",
                column: "BehandlerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "behandler_kontakt_verifiseringer");

            migrationBuilder.DropTable(
                name: "pasient_invitasjoner");

            migrationBuilder.DropTable(
                name: "pasienter");

            migrationBuilder.DropIndex(
                name: "IX_to_faktor_koder_PrincipalType_PrincipalId",
                table: "to_faktor_koder");

            migrationBuilder.DropColumn(
                name: "PrincipalType",
                table: "to_faktor_koder");

            migrationBuilder.DropColumn(
                name: "BrukeravtaleGodkjentUtc",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "BrukeravtaleGodkjentVersjon",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "EpostVerifisertUtc",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "Etternavn",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "Fornavn",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "HprGodkjent",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "HprGodkjentAvAdministratorId",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "HprGodkjentUtc",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "InvitertAvBehandlerId",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "Kontonummer",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "MobilVerifisertUtc",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "Personnummer",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "RegistrertUtc",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "Tittel",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "OpprettetAvBehandlerId",
                table: "behandler_invitasjoner");

            migrationBuilder.RenameColumn(
                name: "PrincipalId",
                table: "to_faktor_koder",
                newName: "AdministratorId");

            migrationBuilder.DropColumn(
                name: "Arbeidsadresse",
                table: "behandlere");

            migrationBuilder.AddColumn<string>(
                name: "FulltNavn",
                table: "behandlere",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<long>(
                name: "InvitertAvAdministratorId",
                table: "behandlere",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "OpprettetAvAdministratorId",
                table: "behandler_invitasjoner",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_to_faktor_koder_AdministratorId",
                table: "to_faktor_koder",
                column: "AdministratorId");
        }
    }
}
