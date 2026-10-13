using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKritiskVarslingPaaDriftsbryter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KritiskVarslingErAktiv",
                table: "DriftsbryterInnstillinger",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KritiskVarslingMobilNr",
                table: "DriftsbryterInnstillinger",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KritiskVarslingErAktiv",
                table: "DriftsbryterInnstillinger");

            migrationBuilder.DropColumn(
                name: "KritiskVarslingMobilNr",
                table: "DriftsbryterInnstillinger");
        }
    }
}
