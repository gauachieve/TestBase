using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestBase.Shared.Migrations
{
    /// <inheritdoc />
    public partial class LeggTilUtbetalingsMottakerKonto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "utbetalings_mottaker_kontoer",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MottakerType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BehandlerId = table.Column<long>(type: "bigint", nullable: true),
                    PartnerId = table.Column<long>(type: "bigint", nullable: true),
                    StripeAccountId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayoutsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DetailsSubmitted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OpprettetUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    OnboardingFullfortUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    SistSynkronisertUtc = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_utbetalings_mottaker_kontoer", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_mottaker_kontoer_MottakerType_BehandlerId_Partne~",
                table: "utbetalings_mottaker_kontoer",
                columns: new[] { "MottakerType", "BehandlerId", "PartnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utbetalings_mottaker_kontoer_StripeAccountId",
                table: "utbetalings_mottaker_kontoer",
                column: "StripeAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "utbetalings_mottaker_kontoer");
        }
    }
}
