using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilDriftsbryterInnstilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriftsbryterInnstillinger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SmsErAktiv = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BetalingErAktiv = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UtbetalingErAktiv = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SistEndretAvUserId = table.Column<long>(type: "bigint", nullable: true),
                    SistEndretUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriftsbryterInnstillinger", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriftsbryterInnstillinger");
        }
    }
}
