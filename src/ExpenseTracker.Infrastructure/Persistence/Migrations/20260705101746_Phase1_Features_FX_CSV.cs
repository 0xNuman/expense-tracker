using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Features_FX_CSV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "import_batch_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "tags",
                table: "transactions",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.CreateTable(
                name: "CachedRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    to_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FXSnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    from_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    to_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    FetchedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FXSnapshots", x => x.SnapshotId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_import_batch_id",
                table: "transactions",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_CachedRates_from_currency_to_currency_FetchedAtUtc",
                table: "CachedRates",
                columns: new[] { "from_currency", "to_currency", "FetchedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FXSnapshots_from_currency_to_currency_FetchedAtUtc",
                table: "FXSnapshots",
                columns: new[] { "from_currency", "to_currency", "FetchedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedRates");

            migrationBuilder.DropTable(
                name: "FXSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_transactions_import_batch_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "import_batch_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "transactions");
        }
    }
}
