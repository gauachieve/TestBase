using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilBankIdSubjekt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankIdSubjekt",
                table: "behandlere",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankIdSubjekt",
                table: "administratorer",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_behandlere_BankIdSubjekt",
                table: "behandlere",
                column: "BankIdSubjekt",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_administratorer_BankIdSubjekt",
                table: "administratorer",
                column: "BankIdSubjekt",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_behandlere_BankIdSubjekt",
                table: "behandlere");

            migrationBuilder.DropIndex(
                name: "IX_administratorer_BankIdSubjekt",
                table: "administratorer");

            migrationBuilder.DropColumn(
                name: "BankIdSubjekt",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "BankIdSubjekt",
                table: "administratorer");
        }
    }
}
