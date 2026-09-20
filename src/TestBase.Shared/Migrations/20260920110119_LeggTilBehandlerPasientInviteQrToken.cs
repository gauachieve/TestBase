using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilBehandlerPasientInviteQrToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasientInviteQrToken",
                table: "behandlere",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_behandlere_PasientInviteQrToken",
                table: "behandlere",
                column: "PasientInviteQrToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_behandlere_PasientInviteQrToken",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "PasientInviteQrToken",
                table: "behandlere");
        }
    }
}
