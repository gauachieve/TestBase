using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AutomatiskPaaminnelseForUbesvarteTester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutomatiskPaaminnelseEtterDager",
                table: "test_tildelinger",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SistPaaminnetUtc",
                table: "test_tildelinger",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AutomatiskPaaminnelseEtterDager",
                table: "program_deltakelser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AutomatiskPaaminnelseEtterDager",
                table: "behandlingsprogrammer",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomatiskPaaminnelseEtterDager",
                table: "test_tildelinger");

            migrationBuilder.DropColumn(
                name: "SistPaaminnetUtc",
                table: "test_tildelinger");

            migrationBuilder.DropColumn(
                name: "AutomatiskPaaminnelseEtterDager",
                table: "program_deltakelser");

            migrationBuilder.DropColumn(
                name: "AutomatiskPaaminnelseEtterDager",
                table: "behandlingsprogrammer");
        }
    }
}
