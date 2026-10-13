using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilAbonnementssystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HarEgetAbonnement",
                table: "behandlere",
                newName: "HarKortLagret");

            migrationBuilder.AddColumn<decimal>(
                name: "MinstePartnerAndelProsent",
                table: "tester",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RettighetshaverKostnadKr",
                table: "tester",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RabattProsent",
                table: "partnere",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StripeAbonnementId",
                table: "partnere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StripeKundeId",
                table: "partnere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "AndelProsent",
                table: "partner_test_andeler",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AbonnementModus",
                table: "behandlere",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "PasientenGratis")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProveperiodeSluttUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeAbonnementId",
                table: "behandlere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StripeKundeId",
                table: "behandlere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "abonnement_oppsett",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SmsKostnadKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EpostKostnadKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EkstraPaaminnelseSmsKostnadKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BetalingsgebyrKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TerapeutGratisProsent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PasientenGratisFlatKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PasientenGratisProsent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ForutsigbartFlatKr = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SistEndretAvUserId = table.Column<long>(type: "bigint", nullable: true),
                    SistEndretUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abonnement_oppsett", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "klinikk_seter_tiers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FraAntallSeter = table.Column<int>(type: "int", nullable: false),
                    TilAntallSeter = table.Column<int>(type: "int", nullable: true),
                    KrPerSeteManed = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_klinikk_seter_tiers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abonnement_oppsett");

            migrationBuilder.DropTable(
                name: "klinikk_seter_tiers");

            migrationBuilder.DropColumn(
                name: "MinstePartnerAndelProsent",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "RettighetshaverKostnadKr",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "RabattProsent",
                table: "partnere");

            migrationBuilder.DropColumn(
                name: "StripeAbonnementId",
                table: "partnere");

            migrationBuilder.DropColumn(
                name: "StripeKundeId",
                table: "partnere");

            migrationBuilder.DropColumn(
                name: "AndelProsent",
                table: "partner_test_andeler");

            migrationBuilder.DropColumn(
                name: "AbonnementModus",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "ProveperiodeSluttUtc",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "StripeAbonnementId",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "StripeKundeId",
                table: "behandlere");

            migrationBuilder.RenameColumn(
                name: "HarKortLagret",
                table: "behandlere",
                newName: "HarEgetAbonnement");
        }
    }
}
