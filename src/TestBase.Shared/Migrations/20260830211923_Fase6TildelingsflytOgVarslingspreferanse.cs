using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Fase6TildelingsflytOgVarslingspreferanse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "TildeltAvBehandlerId",
                table: "test_tildelinger",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "TildeltAvAdministratorId",
                table: "test_tildelinger",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Varslingspreferanse",
                table: "pasienter",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Begge")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "test_kategori_koblinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TestId = table.Column<long>(type: "bigint", nullable: false),
                    TestKategoriId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_kategori_koblinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "test_kategorier",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Navn = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_kategorier", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_test_kategori_koblinger_TestId_TestKategoriId",
                table: "test_kategori_koblinger",
                columns: new[] { "TestId", "TestKategoriId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_kategori_koblinger_TestKategoriId",
                table: "test_kategori_koblinger",
                column: "TestKategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_test_kategorier_Navn",
                table: "test_kategorier",
                column: "Navn",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_kategori_koblinger");

            migrationBuilder.DropTable(
                name: "test_kategorier");

            migrationBuilder.DropColumn(
                name: "TildeltAvAdministratorId",
                table: "test_tildelinger");

            migrationBuilder.DropColumn(
                name: "Varslingspreferanse",
                table: "pasienter");

            migrationBuilder.AlterColumn<long>(
                name: "TildeltAvBehandlerId",
                table: "test_tildelinger",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
