using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_memberships_tenants_tenant_id",
                table: "tenant_memberships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transfers",
                table: "transfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transactions",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenants",
                table: "tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenant_memberships",
                table: "tenant_memberships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_recurring_rules",
                table: "recurring_rules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_recurring_execution_logs",
                table: "recurring_execution_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_passkey_credentials",
                table: "passkey_credentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_magic_link_tokens",
                table: "magic_link_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fx_snapshots",
                table: "fx_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_categories",
                table: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cached_rates",
                table: "cached_rates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_accounts",
                table: "accounts");

            migrationBuilder.RenameIndex(
                name: "IX_users_normalized_email",
                table: "users",
                newName: "ix_users_normalized_email");

            migrationBuilder.RenameIndex(
                name: "IX_transfers_tenant_id",
                table: "transfers",
                newName: "ix_transfers_tenant_id");

            migrationBuilder.RenameIndex(
                name: "IX_transfers_source_account_id",
                table: "transfers",
                newName: "ix_transfers_source_account_id");

            migrationBuilder.RenameIndex(
                name: "IX_transfers_destination_account_id",
                table: "transfers",
                newName: "ix_transfers_destination_account_id");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_tenant_id_account_id",
                table: "transactions",
                newName: "ix_transactions_tenant_id_account_id");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_tenant_id",
                table: "transactions",
                newName: "ix_transactions_tenant_id");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_occurred_on",
                table: "transactions",
                newName: "ix_transactions_occurred_on");

            migrationBuilder.RenameIndex(
                name: "IX_transactions_import_batch_id",
                table: "transactions",
                newName: "ix_transactions_import_batch_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_memberships_user_id",
                table: "tenant_memberships",
                newName: "ix_tenant_memberships_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_memberships_tenant_id_user_id",
                table: "tenant_memberships",
                newName: "ix_tenant_memberships_tenant_id_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                newName: "ix_refresh_tokens_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                newName: "ix_refresh_tokens_token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_family_id",
                table: "refresh_tokens",
                newName: "ix_refresh_tokens_family_id");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_expires_at_utc",
                table: "refresh_tokens",
                newName: "ix_refresh_tokens_expires_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_recurring_rules_tenant_id",
                table: "recurring_rules",
                newName: "ix_recurring_rules_tenant_id");

            migrationBuilder.RenameIndex(
                name: "IX_recurring_rules_next_run_utc",
                table: "recurring_rules",
                newName: "ix_recurring_rules_next_run_utc");

            migrationBuilder.RenameIndex(
                name: "IX_recurring_execution_logs_rule_id",
                table: "recurring_execution_logs",
                newName: "ix_recurring_execution_logs_rule_id");

            migrationBuilder.RenameIndex(
                name: "IX_passkey_credentials_user_id",
                table: "passkey_credentials",
                newName: "ix_passkey_credentials_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_passkey_credentials_credential_id",
                table: "passkey_credentials",
                newName: "ix_passkey_credentials_credential_id");

            migrationBuilder.RenameIndex(
                name: "IX_magic_link_tokens_token_hash",
                table: "magic_link_tokens",
                newName: "ix_magic_link_tokens_token_hash");

            migrationBuilder.RenameIndex(
                name: "IX_magic_link_tokens_normalized_email",
                table: "magic_link_tokens",
                newName: "ix_magic_link_tokens_normalized_email");

            migrationBuilder.RenameIndex(
                name: "IX_magic_link_tokens_expires_at_utc",
                table: "magic_link_tokens",
                newName: "ix_magic_link_tokens_expires_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_fx_snapshots_from_currency_to_currency_fetched_at_utc",
                table: "fx_snapshots",
                newName: "ix_fx_snapshots_from_currency_to_currency_fetched_at_utc");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "categories",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "categories",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Kind",
                table: "categories",
                newName: "kind");

            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "categories",
                newName: "icon");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "categories",
                newName: "color");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "categories",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "categories",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "categories",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "categories",
                newName: "parent_id");

            migrationBuilder.RenameColumn(
                name: "IsArchived",
                table: "categories",
                newName: "is_archived");

            migrationBuilder.RenameIndex(
                name: "IX_categories_TenantId_ParentId_Name",
                table: "categories",
                newName: "ix_categories_tenant_id_parent_id_name");

            migrationBuilder.RenameIndex(
                name: "IX_cached_rates_from_currency_to_currency_fetched_at_utc",
                table: "cached_rates",
                newName: "ix_cached_rates_from_currency_to_currency_fetched_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_accounts_tenant_id_name",
                table: "accounts",
                newName: "ix_accounts_tenant_id_name");

            migrationBuilder.RenameIndex(
                name: "IX_accounts_tenant_id",
                table: "accounts",
                newName: "ix_accounts_tenant_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_transfers",
                table: "transfers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_transactions",
                table: "transactions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tenants",
                table: "tenants",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tenant_memberships",
                table: "tenant_memberships",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_refresh_tokens",
                table: "refresh_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_recurring_rules",
                table: "recurring_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_recurring_execution_logs",
                table: "recurring_execution_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_passkey_credentials",
                table: "passkey_credentials",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_magic_link_tokens",
                table: "magic_link_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fx_snapshots",
                table: "fx_snapshots",
                column: "snapshot_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_categories",
                table: "categories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cached_rates",
                table: "cached_rates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_accounts",
                table: "accounts",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_memberships_tenants_tenant_id",
                table: "tenant_memberships",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tenant_memberships_tenants_tenant_id",
                table: "tenant_memberships");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_transfers",
                table: "transfers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_transactions",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tenants",
                table: "tenants");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tenant_memberships",
                table: "tenant_memberships");

            migrationBuilder.DropPrimaryKey(
                name: "pk_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_recurring_rules",
                table: "recurring_rules");

            migrationBuilder.DropPrimaryKey(
                name: "pk_recurring_execution_logs",
                table: "recurring_execution_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_passkey_credentials",
                table: "passkey_credentials");

            migrationBuilder.DropPrimaryKey(
                name: "pk_magic_link_tokens",
                table: "magic_link_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fx_snapshots",
                table: "fx_snapshots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_categories",
                table: "categories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_cached_rates",
                table: "cached_rates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_accounts",
                table: "accounts");

            migrationBuilder.RenameIndex(
                name: "ix_users_normalized_email",
                table: "users",
                newName: "IX_users_normalized_email");

            migrationBuilder.RenameIndex(
                name: "ix_transfers_tenant_id",
                table: "transfers",
                newName: "IX_transfers_tenant_id");

            migrationBuilder.RenameIndex(
                name: "ix_transfers_source_account_id",
                table: "transfers",
                newName: "IX_transfers_source_account_id");

            migrationBuilder.RenameIndex(
                name: "ix_transfers_destination_account_id",
                table: "transfers",
                newName: "IX_transfers_destination_account_id");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_tenant_id_account_id",
                table: "transactions",
                newName: "IX_transactions_tenant_id_account_id");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_tenant_id",
                table: "transactions",
                newName: "IX_transactions_tenant_id");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_occurred_on",
                table: "transactions",
                newName: "IX_transactions_occurred_on");

            migrationBuilder.RenameIndex(
                name: "ix_transactions_import_batch_id",
                table: "transactions",
                newName: "IX_transactions_import_batch_id");

            migrationBuilder.RenameIndex(
                name: "ix_tenant_memberships_user_id",
                table: "tenant_memberships",
                newName: "IX_tenant_memberships_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_tenant_memberships_tenant_id_user_id",
                table: "tenant_memberships",
                newName: "IX_tenant_memberships_tenant_id_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_token_hash");

            migrationBuilder.RenameIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_family_id");

            migrationBuilder.RenameIndex(
                name: "ix_refresh_tokens_expires_at_utc",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_expires_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_recurring_rules_tenant_id",
                table: "recurring_rules",
                newName: "IX_recurring_rules_tenant_id");

            migrationBuilder.RenameIndex(
                name: "ix_recurring_rules_next_run_utc",
                table: "recurring_rules",
                newName: "IX_recurring_rules_next_run_utc");

            migrationBuilder.RenameIndex(
                name: "ix_recurring_execution_logs_rule_id",
                table: "recurring_execution_logs",
                newName: "IX_recurring_execution_logs_rule_id");

            migrationBuilder.RenameIndex(
                name: "ix_passkey_credentials_user_id",
                table: "passkey_credentials",
                newName: "IX_passkey_credentials_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_passkey_credentials_credential_id",
                table: "passkey_credentials",
                newName: "IX_passkey_credentials_credential_id");

            migrationBuilder.RenameIndex(
                name: "ix_magic_link_tokens_token_hash",
                table: "magic_link_tokens",
                newName: "IX_magic_link_tokens_token_hash");

            migrationBuilder.RenameIndex(
                name: "ix_magic_link_tokens_normalized_email",
                table: "magic_link_tokens",
                newName: "IX_magic_link_tokens_normalized_email");

            migrationBuilder.RenameIndex(
                name: "ix_magic_link_tokens_expires_at_utc",
                table: "magic_link_tokens",
                newName: "IX_magic_link_tokens_expires_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_fx_snapshots_from_currency_to_currency_fetched_at_utc",
                table: "fx_snapshots",
                newName: "IX_fx_snapshots_from_currency_to_currency_fetched_at_utc");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "categories",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "categories",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "kind",
                table: "categories",
                newName: "Kind");

            migrationBuilder.RenameColumn(
                name: "icon",
                table: "categories",
                newName: "Icon");

            migrationBuilder.RenameColumn(
                name: "color",
                table: "categories",
                newName: "Color");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "categories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "categories",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "categories",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "parent_id",
                table: "categories",
                newName: "ParentId");

            migrationBuilder.RenameColumn(
                name: "is_archived",
                table: "categories",
                newName: "IsArchived");

            migrationBuilder.RenameIndex(
                name: "ix_categories_tenant_id_parent_id_name",
                table: "categories",
                newName: "IX_categories_TenantId_ParentId_Name");

            migrationBuilder.RenameIndex(
                name: "ix_cached_rates_from_currency_to_currency_fetched_at_utc",
                table: "cached_rates",
                newName: "IX_cached_rates_from_currency_to_currency_fetched_at_utc");

            migrationBuilder.RenameIndex(
                name: "ix_accounts_tenant_id_name",
                table: "accounts",
                newName: "IX_accounts_tenant_id_name");

            migrationBuilder.RenameIndex(
                name: "ix_accounts_tenant_id",
                table: "accounts",
                newName: "IX_accounts_tenant_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transfers",
                table: "transfers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transactions",
                table: "transactions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenants",
                table: "tenants",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenant_memberships",
                table: "tenant_memberships",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_recurring_rules",
                table: "recurring_rules",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_recurring_execution_logs",
                table: "recurring_execution_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_passkey_credentials",
                table: "passkey_credentials",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_magic_link_tokens",
                table: "magic_link_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fx_snapshots",
                table: "fx_snapshots",
                column: "snapshot_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_categories",
                table: "categories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cached_rates",
                table: "cached_rates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_accounts",
                table: "accounts",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_memberships_tenants_tenant_id",
                table: "tenant_memberships",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
