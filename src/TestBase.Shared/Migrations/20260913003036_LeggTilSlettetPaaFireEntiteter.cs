using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilSlettetPaaFireEntiteter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ErSlettet",
                table: "pasienter",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SlettetUtc",
                table: "pasienter",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErSlettet",
                table: "partnere",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SlettetUtc",
                table: "partnere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErSlettet",
                table: "behandlere",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SlettetUtc",
                table: "behandlere",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ErSlettet",
                table: "administratorer",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SlettetUtc",
                table: "administratorer",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErSlettet",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "SlettetUtc",
                table: "pasienter");

            migrationBuilder.DropColumn(
                name: "ErSlettet",
                table: "partnere");

            migrationBuilder.DropColumn(
                name: "SlettetUtc",
                table: "partnere");

            migrationBuilder.DropColumn(
                name: "ErSlettet",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "SlettetUtc",
                table: "behandlere");

            migrationBuilder.DropColumn(
                name: "ErSlettet",
                table: "administratorer");

            migrationBuilder.DropColumn(
                name: "SlettetUtc",
                table: "administratorer");
        }
    }
}
