using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilProfilFullforingToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilFullforingToken",
                table: "pasienter",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_pasienter_ProfilFullforingToken",
                table: "pasienter",
                column: "ProfilFullforingToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pasienter_ProfilFullforingToken",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "ProfilFullforingToken",
                table: "pasienter");
        }
    }
}
