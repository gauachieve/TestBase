using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilKostnadPerGjennomforing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HarKostnadPerGjennomforing",
                table: "tester",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HarKostnadPerGjennomforing",
                table: "tester");
        }
    }
}
