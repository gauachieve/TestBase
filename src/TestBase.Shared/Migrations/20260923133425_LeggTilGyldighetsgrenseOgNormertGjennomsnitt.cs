using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilGyldighetsgrenseOgNormertGjennomsnitt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaksUbesvartProsent",
                table: "tester",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NormertGjennomsnitt",
                table: "test_ledd",
                type: "decimal(10,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaksUbesvartProsent",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "NormertGjennomsnitt",
                table: "test_ledd");
        }
    }
}
