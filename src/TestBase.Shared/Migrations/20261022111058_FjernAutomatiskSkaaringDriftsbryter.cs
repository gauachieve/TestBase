using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class FjernAutomatiskSkaaringDriftsbryter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomatiskSkaaringErAktiv",
                table: "DriftsbryterInnstillinger");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutomatiskSkaaringErAktiv",
                table: "DriftsbryterInnstillinger",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
