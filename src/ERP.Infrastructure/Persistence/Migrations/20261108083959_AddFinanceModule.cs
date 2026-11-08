using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCode = table.Column<string>(type: "text", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    NormalBalance = table.Column<string>(type: "text", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsHeader = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chart_of_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chart_of_accounts_chart_of_accounts_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalSchema: "finance",
                        principalTable: "chart_of_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chart_of_accounts_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_periods",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_periods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fiscal_periods_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalNumber = table.Column<string>(type: "text", nullable: false),
                    JournalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReferenceType = table.Column<string>(type: "text", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalDebit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_journal_entries_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "master",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entries_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_bank_accounts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: true),
                    AccountNumber = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_bank_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_bank_accounts_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "master",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cash_bank_accounts_chart_of_accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "finance",
                        principalTable: "chart_of_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cash_bank_accounts_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_journal_lines_chart_of_accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "finance",
                        principalTable: "chart_of_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_lines_journal_entries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "finance",
                        principalTable: "journal_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CashBankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<string>(type: "text", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentType = table.Column<string>(type: "text", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "master",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_cash_bank_accounts_CashBankAccountId",
                        column: x => x.CashBankAccountId,
                        principalSchema: "finance",
                        principalTable: "cash_bank_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_allocations_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "finance",
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_bank_accounts_AccountId",
                schema: "finance",
                table: "cash_bank_accounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_bank_accounts_BranchId",
                schema: "finance",
                table: "cash_bank_accounts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_bank_accounts_CompanyId_Code",
                schema: "finance",
                table: "cash_bank_accounts",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chart_of_accounts_CompanyId_AccountCode",
                schema: "finance",
                table: "chart_of_accounts",
                columns: new[] { "CompanyId", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chart_of_accounts_ParentAccountId",
                schema: "finance",
                table: "chart_of_accounts",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_periods_CompanyId_FiscalYear_PeriodNumber",
                schema: "finance",
                table: "fiscal_periods",
                columns: new[] { "CompanyId", "FiscalYear", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_BranchId",
                schema: "finance",
                table: "journal_entries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_CompanyId",
                schema: "finance",
                table: "journal_entries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_JournalNumber",
                schema: "finance",
                table: "journal_entries",
                column: "JournalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_AccountId",
                schema: "finance",
                table: "journal_lines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_JournalEntryId",
                schema: "finance",
                table: "journal_lines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_PaymentId",
                schema: "finance",
                table: "payment_allocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_BranchId",
                schema: "finance",
                table: "payments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_CashBankAccountId",
                schema: "finance",
                table: "payments",
                column: "CashBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_CompanyId",
                schema: "finance",
                table: "payments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaymentNumber",
                schema: "finance",
                table: "payments",
                column: "PaymentNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiscal_periods",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "journal_lines",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "journal_entries",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "cash_bank_accounts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "chart_of_accounts",
                schema: "finance");
        }
    }
}
