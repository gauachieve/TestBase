using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class RenameKritiskVarslingTilPushover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KritiskVarslingMobilNr",
                table: "DriftsbryterInnstillinger",
                newName: "KritiskVarslingPushoverBrukerKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KritiskVarslingPushoverBrukerKey",
                table: "DriftsbryterInnstillinger",
                newName: "KritiskVarslingMobilNr");
        }
    }
}
