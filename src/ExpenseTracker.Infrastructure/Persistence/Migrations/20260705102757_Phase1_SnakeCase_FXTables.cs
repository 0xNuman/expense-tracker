using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_SnakeCase_FXTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FXSnapshots",
                table: "FXSnapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CachedRates",
                table: "CachedRates");

            migrationBuilder.RenameTable(
                name: "FXSnapshots",
                newName: "fx_snapshots");

            migrationBuilder.RenameTable(
                name: "CachedRates",
                newName: "cached_rates");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "fx_snapshots",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "Rate",
                table: "fx_snapshots",
                newName: "rate");

            migrationBuilder.RenameColumn(
                name: "Method",
                table: "fx_snapshots",
                newName: "method");

            migrationBuilder.RenameColumn(
                name: "FetchedAtUtc",
                table: "fx_snapshots",
                newName: "fetched_at_utc");

            migrationBuilder.RenameColumn(
                name: "SnapshotId",
                table: "fx_snapshots",
                newName: "snapshot_id");

            migrationBuilder.RenameIndex(
                name: "IX_FXSnapshots_from_currency_to_currency_FetchedAtUtc",
                table: "fx_snapshots",
                newName: "IX_fx_snapshots_from_currency_to_currency_fetched_at_utc");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "cached_rates",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "Rate",
                table: "cached_rates",
                newName: "rate");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "cached_rates",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "FetchedAtUtc",
                table: "cached_rates",
                newName: "fetched_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_CachedRates_from_currency_to_currency_FetchedAtUtc",
                table: "cached_rates",
                newName: "IX_cached_rates_from_currency_to_currency_fetched_at_utc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fx_snapshots",
                table: "fx_snapshots",
                column: "snapshot_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cached_rates",
                table: "cached_rates",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_fx_snapshots",
                table: "fx_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cached_rates",
                table: "cached_rates");

            migrationBuilder.RenameTable(
                name: "fx_snapshots",
                newName: "FXSnapshots");

            migrationBuilder.RenameTable(
                name: "cached_rates",
                newName: "CachedRates");

            migrationBuilder.RenameColumn(
                name: "source",
                table: "FXSnapshots",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "rate",
                table: "FXSnapshots",
                newName: "Rate");

            migrationBuilder.RenameColumn(
                name: "method",
                table: "FXSnapshots",
                newName: "Method");

            migrationBuilder.RenameColumn(
                name: "fetched_at_utc",
                table: "FXSnapshots",
                newName: "FetchedAtUtc");

            migrationBuilder.RenameColumn(
                name: "snapshot_id",
                table: "FXSnapshots",
                newName: "SnapshotId");

            migrationBuilder.RenameIndex(
                name: "IX_fx_snapshots_from_currency_to_currency_fetched_at_utc",
                table: "FXSnapshots",
                newName: "IX_FXSnapshots_from_currency_to_currency_FetchedAtUtc");

            migrationBuilder.RenameColumn(
                name: "source",
                table: "CachedRates",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "rate",
                table: "CachedRates",
                newName: "Rate");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CachedRates",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "fetched_at_utc",
                table: "CachedRates",
                newName: "FetchedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_cached_rates_from_currency_to_currency_fetched_at_utc",
                table: "CachedRates",
                newName: "IX_CachedRates_from_currency_to_currency_FetchedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FXSnapshots",
                table: "FXSnapshots",
                column: "SnapshotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CachedRates",
                table: "CachedRates",
                column: "Id");
        }
    }
}
