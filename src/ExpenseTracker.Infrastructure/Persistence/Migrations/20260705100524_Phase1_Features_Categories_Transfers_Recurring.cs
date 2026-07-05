using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Features_Categories_Transfers_Recurring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recurring_execution_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_for_utc = table.Column<DateOnly>(type: "date", nullable: false),
                    posted_txn_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    executed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_execution_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recurring_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    rule_kind = table.Column<string>(type: "text", nullable: false),
                    cadence = table.Column<string>(type: "text", nullable: false),
                    interval = table.Column<int>(type: "integer", nullable: false),
                    days_of_week = table.Column<byte[]>(type: "bytea", nullable: true),
                    day_of_month = table.Column<int>(type: "integer", nullable: true),
                    month_of_year = table.Column<int>(type: "integer", nullable: true),
                    start_date_utc = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date_utc = table.Column<DateOnly>(type: "date", nullable: true),
                    next_run_utc = table.Column<DateOnly>(type: "date", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    counterpart_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fx_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    memo_pattern = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
                    auto_post = table.Column<bool>(type: "boolean", nullable: false),
                    grace_days = table.Column<int>(type: "integer", nullable: false),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_run_txn_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed = table.Column<bool>(type: "boolean", nullable: false),
                    amount_account_currency = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    destination_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    destination_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    fx_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_on_utc = table.Column<DateOnly>(type: "date", nullable: false),
                    memo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ref_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    voided_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId_ParentId_Name",
                table: "Categories",
                columns: new[] { "TenantId", "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_execution_logs_rule_id",
                table: "recurring_execution_logs",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_rules_next_run_utc",
                table: "recurring_rules",
                column: "next_run_utc");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_rules_tenant_id",
                table: "recurring_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_destination_account_id",
                table: "transfers",
                column: "destination_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_source_account_id",
                table: "transfers",
                column: "source_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_tenant_id",
                table: "transfers",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "recurring_execution_logs");

            migrationBuilder.DropTable(
                name: "recurring_rules");

            migrationBuilder.DropTable(
                name: "transfers");
        }
    }
}
