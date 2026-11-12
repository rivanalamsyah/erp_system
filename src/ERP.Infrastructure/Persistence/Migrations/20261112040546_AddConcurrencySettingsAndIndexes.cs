using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencySettingsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vendor_bills_CompanyId",
                schema: "procurement",
                table: "vendor_bills");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_CompanyId",
                schema: "sales",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_CompanyId",
                schema: "finance",
                table: "journal_entries");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "inventory",
                table: "stock_balances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "master",
                table: "document_sequences",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "AlertLowStock",
                schema: "master",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AlertOverdueInvoices",
                schema: "master",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultCreditLimit",
                schema: "master",
                table: "companies",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 50000000m);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPaymentTermsDays",
                schema: "master",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultVatRate",
                schema: "master",
                table: "companies",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 11.0m);

            migrationBuilder.AddColumn<bool>(
                name: "EnforceStrictCreditLimit",
                schema: "master",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalStartMonth",
                schema: "master",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OverdueReminderDays",
                schema: "master",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<int>(
                name: "StockWarningThresholdPercentage",
                schema: "master",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId_Status_BillDate",
                schema: "procurement",
                table: "vendor_bills",
                columns: new[] { "CompanyId", "Status", "BillDate" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CompanyId_Status_InvoiceDate",
                schema: "sales",
                table: "sales_invoices",
                columns: new[] { "CompanyId", "Status", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_CompanyId_Status_JournalDate",
                schema: "finance",
                table: "journal_entries",
                columns: new[] { "CompanyId", "Status", "JournalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CompanyId_CreatedAtUtc",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityName_Action",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "EntityName", "Action" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vendor_bills_CompanyId_Status_BillDate",
                schema: "procurement",
                table: "vendor_bills");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_CompanyId_Status_InvoiceDate",
                schema: "sales",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_CompanyId_Status_JournalDate",
                schema: "finance",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_CompanyId_CreatedAtUtc",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_EntityName_Action",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "inventory",
                table: "stock_balances");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "master",
                table: "document_sequences");

            migrationBuilder.DropColumn(
                name: "AlertLowStock",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AlertOverdueInvoices",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "DefaultCreditLimit",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "DefaultPaymentTermsDays",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "DefaultVatRate",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "EnforceStrictCreditLimit",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "FiscalStartMonth",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "OverdueReminderDays",
                schema: "master",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "StockWarningThresholdPercentage",
                schema: "master",
                table: "companies");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId",
                schema: "procurement",
                table: "vendor_bills",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CompanyId",
                schema: "sales",
                table: "sales_invoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_CompanyId",
                schema: "finance",
                table: "journal_entries",
                column: "CompanyId");
        }
    }
}
