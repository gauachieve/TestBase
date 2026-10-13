using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class FullforAbonnementProsentCutover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HarKostnadPerGjennomforing",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "MinstePartnerAndelKr",
                table: "tester");

            migrationBuilder.DropColumn(
                name: "AndelKr",
                table: "partner_test_andeler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HarKostnadPerGjennomforing",
                table: "tester",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MinstePartnerAndelKr",
                table: "tester",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AndelKr",
                table: "partner_test_andeler",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
