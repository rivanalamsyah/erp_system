using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Finance;
using ERP.Domain.Entities.Hr;
using ERP.Domain.Entities.Master;
using ERP.Domain.Entities.Procurement;
using ERP.Domain.Entities.Sales;
using ERP.Shared;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Services;

public class AccountingPostingService(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService) : IAccountingPostingService
{
    private static readonly (string Code, string Name, AccountType Type, NormalBalance Balance)[] StandardAccounts =
    [
        ("1-1100", "Kas Operasional", AccountType.Asset, NormalBalance.DR),
        ("1-1200", "Bank BCA Giro Operasional", AccountType.Asset, NormalBalance.DR),
        ("1-1300", "Piutang Usaha (Accounts Receivable)", AccountType.Asset, NormalBalance.DR),
        ("1-1400", "Persediaan Barang Dagang (Inventory)", AccountType.Asset, NormalBalance.DR),
        ("1-1500", "PPN Masukan (Prepaid VAT)", AccountType.Asset, NormalBalance.DR),
        ("2-1100", "Hutang Usaha (Accounts Payable)", AccountType.Liability, NormalBalance.CR),
        ("2-1200", "Hutang Gaji & Upah Karyawan", AccountType.Liability, NormalBalance.CR),
        ("2-1300", "PPN Keluaran (VAT Output)", AccountType.Liability, NormalBalance.CR),
        ("4-1000", "Pendapatan Penjualan Produk", AccountType.Revenue, NormalBalance.CR),
        ("5-1000", "Beban Pokok Penjualan (COGS)", AccountType.Expense, NormalBalance.DR),
        ("6-1100", "Beban Gaji, Tunjangan & Upah", AccountType.Expense, NormalBalance.DR)
    ];

    public async Task<Result> EnsureFiscalPeriodOpenAsync(
        Guid companyId,
        DateOnly transactionDate,
        CancellationToken cancellationToken = default)
    {
        var period = context.FiscalPeriods.Local
            .FirstOrDefault(p => p.CompanyId == companyId
                && p.FiscalYear == transactionDate.Year
                && p.PeriodNumber == transactionDate.Month)
            ?? await context.FiscalPeriods
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CompanyId == companyId
                    && p.FiscalYear == transactionDate.Year
                    && p.PeriodNumber == transactionDate.Month, cancellationToken);

        if (period != null && period.IsClosed)
        {
            return Result.Failure(Error.Validation(
                "BR-ACC-002",
                $"Transaksi ditolak: Periode fiskal {transactionDate.Month:D2}/{transactionDate.Year} telah ditutup (Closed Fiscal Period)."));
        }

        return Result.Success();
    }

    public async Task<Result<Guid>> PostGoodsReceiptJournalAsync(
        GoodsReceipt grn,
        decimal totalReceiptValue,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var periodCheck = await EnsureFiscalPeriodOpenAsync(grn.CompanyId, grn.ReceiptDate, cancellationToken);
        if (periodCheck.IsFailure)
            return Result<Guid>.Failure(periodCheck.Error);

        var roundedTotal = Math.Round(totalReceiptValue, 2);
        if (roundedTotal <= 0)
            return Result<Guid>.Success(Guid.Empty);

        var existingId = await FindExistingPostedJournalIdAsync(grn.CompanyId, "GRN", grn.ReceiptNumber, cancellationToken);
        if (existingId.HasValue)
            return Result<Guid>.Success(existingId.Value);

        var accounts = await EnsureAndGetAccountsAsync(grn.CompanyId, userId, cancellationToken);
        var journalNumber = await sequenceService.NextDocumentNumberAsync(grn.CompanyId, grn.BranchId, "JV", cancellationToken);

        var entry = JournalEntry.Create(
            companyId: grn.CompanyId,
            branchId: grn.BranchId,
            journalNumber: journalNumber,
            journalDate: grn.ReceiptDate,
            referenceType: "GRN",
            referenceNumber: grn.ReceiptNumber,
            description: $"Auto-Posting Penerimaan Barang {grn.ReceiptNumber} (SJ: {grn.DeliveryNoteNumber})",
            createdByUserId: userId);

        entry.AddDebitLine(accounts["1-1400"].Id, roundedTotal, $"Persediaan Masuk {grn.ReceiptNumber}");
        entry.AddCreditLine(accounts["2-1200"].Id, roundedTotal, $"GR/IR Accrual {grn.ReceiptNumber}");
        entry.Post(userId);

        context.JournalEntries.Add(entry);
        return Result<Guid>.Success(entry.Id);
    }

    public async Task<Result<Guid>> PostVendorBillJournalAsync(
        VendorBill bill,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var periodCheck = await EnsureFiscalPeriodOpenAsync(bill.CompanyId, bill.BillDate, cancellationToken);
        if (periodCheck.IsFailure)
            return Result<Guid>.Failure(periodCheck.Error);

        var roundedTotal = Math.Round(bill.TotalAmount, 2);
        if (roundedTotal <= 0)
            return Result<Guid>.Success(Guid.Empty);

        var existingId = await FindExistingPostedJournalIdAsync(bill.CompanyId, "BILL", bill.BillNumber, cancellationToken);
        if (existingId.HasValue)
            return Result<Guid>.Success(existingId.Value);

        var accounts = await EnsureAndGetAccountsAsync(bill.CompanyId, userId, cancellationToken);
        var journalNumber = await sequenceService.NextDocumentNumberAsync(bill.CompanyId, bill.BranchId, "JV", cancellationToken);

        var entry = JournalEntry.Create(
            companyId: bill.CompanyId,
            branchId: bill.BranchId,
            journalNumber: journalNumber,
            journalDate: bill.BillDate,
            referenceType: "BILL",
            referenceNumber: bill.BillNumber,
            description: $"Auto-Posting Tagihan Vendor {bill.BillNumber} (Faktur: {bill.VendorInvoiceNumber})",
            createdByUserId: userId);

        var taxAmount = bill.PurchaseOrder != null ? Math.Round(bill.PurchaseOrder.TaxAmount, 2) : 0m;
        var netAmount = roundedTotal - taxAmount;

        if (taxAmount > 0 && netAmount > 0)
        {
            entry.AddDebitLine(accounts["2-1200"].Id, netAmount, $"Clearing GR/IR / Pembelian {bill.BillNumber}");
            entry.AddDebitLine(accounts["1-1500"].Id, taxAmount, $"PPN Masukan {bill.BillNumber}");
        }
        else
        {
            entry.AddDebitLine(accounts["2-1200"].Id, roundedTotal, $"Clearing GR/IR / Pembelian {bill.BillNumber}");
        }

        entry.AddCreditLine(accounts["2-1100"].Id, roundedTotal, $"Hutang Usaha Vendor {bill.BillNumber}");
        entry.Post(userId);

        context.JournalEntries.Add(entry);
        return Result<Guid>.Success(entry.Id);
    }

    public async Task<Result<Guid>> PostDeliveryCogsJournalAsync(
        Delivery delivery,
        decimal totalCogsAmount,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var periodCheck = await EnsureFiscalPeriodOpenAsync(delivery.CompanyId, delivery.DeliveryDate, cancellationToken);
        if (periodCheck.IsFailure)
            return Result<Guid>.Failure(periodCheck.Error);

        var roundedCogs = Math.Round(totalCogsAmount, 2);
        if (roundedCogs <= 0)
            return Result<Guid>.Success(Guid.Empty);

        var existingId = await FindExistingPostedJournalIdAsync(delivery.CompanyId, "DO", delivery.DeliveryNumber, cancellationToken);
        if (existingId.HasValue)
            return Result<Guid>.Success(existingId.Value);

        var accounts = await EnsureAndGetAccountsAsync(delivery.CompanyId, userId, cancellationToken);
        var journalNumber = await sequenceService.NextDocumentNumberAsync(delivery.CompanyId, delivery.BranchId, "JV", cancellationToken);

        var entry = JournalEntry.Create(
            companyId: delivery.CompanyId,
            branchId: delivery.BranchId,
            journalNumber: journalNumber,
            journalDate: delivery.DeliveryDate,
            referenceType: "DO",
            referenceNumber: delivery.DeliveryNumber,
            description: $"Auto-Posting HPP / COGS Pengiriman Barang {delivery.DeliveryNumber}",
            createdByUserId: userId);

        entry.AddDebitLine(accounts["5-1000"].Id, roundedCogs, $"Beban Pokok Penjualan {delivery.DeliveryNumber}");
        entry.AddCreditLine(accounts["1-1400"].Id, roundedCogs, $"Pengeluaran Persediaan {delivery.DeliveryNumber}");
        entry.Post(userId);

        context.JournalEntries.Add(entry);
        return Result<Guid>.Success(entry.Id);
    }

    public async Task<Result<Guid>> PostSalesInvoiceJournalAsync(
        SalesInvoice invoice,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var periodCheck = await EnsureFiscalPeriodOpenAsync(invoice.CompanyId, invoice.InvoiceDate, cancellationToken);
        if (periodCheck.IsFailure)
            return Result<Guid>.Failure(periodCheck.Error);

        var roundedTotal = Math.Round(invoice.TotalAmount, 2);
        if (roundedTotal <= 0)
            return Result<Guid>.Success(Guid.Empty);

        var existingId = await FindExistingPostedJournalIdAsync(invoice.CompanyId, "INV", invoice.InvoiceNumber, cancellationToken);
        if (existingId.HasValue)
            return Result<Guid>.Success(existingId.Value);

        var accounts = await EnsureAndGetAccountsAsync(invoice.CompanyId, userId, cancellationToken);
        var journalNumber = await sequenceService.NextDocumentNumberAsync(invoice.CompanyId, invoice.BranchId, "JV", cancellationToken);

        var entry = JournalEntry.Create(
            companyId: invoice.CompanyId,
            branchId: invoice.BranchId,
            journalNumber: journalNumber,
            journalDate: invoice.InvoiceDate,
            referenceType: "INV",
            referenceNumber: invoice.InvoiceNumber,
            description: $"Auto-Posting Faktur Penjualan {invoice.InvoiceNumber}",
            createdByUserId: userId);

        var taxAmount = Math.Round(invoice.TaxAmount, 2);
        var revenueAmount = roundedTotal - taxAmount;

        entry.AddDebitLine(accounts["1-1300"].Id, roundedTotal, $"Piutang Usaha {invoice.InvoiceNumber}");

        if (revenueAmount > 0)
            entry.AddCreditLine(accounts["4-1000"].Id, revenueAmount, $"Pendapatan Penjualan {invoice.InvoiceNumber}");

        if (taxAmount > 0)
            entry.AddCreditLine(accounts["2-1300"].Id, taxAmount, $"PPN Keluaran {invoice.InvoiceNumber}");

        entry.Post(userId);

        context.JournalEntries.Add(entry);
        return Result<Guid>.Success(entry.Id);
    }

    public async Task<Result<Guid>> PostPaymentJournalAsync(
        Payment payment,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var periodCheck = await EnsureFiscalPeriodOpenAsync(payment.CompanyId, payment.PaymentDate, cancellationToken);
        if (periodCheck.IsFailure)
            return Result<Guid>.Failure(periodCheck.Error);

        var roundedTotal = Math.Round(payment.TotalAmount, 2);
        if (roundedTotal <= 0)
            return Result<Guid>.Success(Guid.Empty);

        var existingId = await FindExistingPostedJournalIdAsync(payment.CompanyId, "PAY", payment.PaymentNumber, cancellationToken);
        if (existingId.HasValue)
            return Result<Guid>.Success(existingId.Value);

        var accounts = await EnsureAndGetAccountsAsync(payment.CompanyId, userId, cancellationToken);

        var cashBankCoaId = context.CashBankAccounts.Local
            .Where(cb => cb.Id == payment.CashBankAccountId)
            .Select(cb => (Guid?)cb.AccountId)
            .FirstOrDefault()
            ?? await context.CashBankAccounts
                .AsNoTracking()
                .Where(cb => cb.Id == payment.CashBankAccountId)
                .Select(cb => (Guid?)cb.AccountId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? accounts["1-1200"].Id;

        var journalNumber = await sequenceService.NextDocumentNumberAsync(payment.CompanyId, payment.BranchId, "JV", cancellationToken);

        var entry = JournalEntry.Create(
            companyId: payment.CompanyId,
            branchId: payment.BranchId,
            journalNumber: journalNumber,
            journalDate: payment.PaymentDate,
            referenceType: "PAY",
            referenceNumber: payment.PaymentNumber,
            description: payment.PaymentType == PaymentType.CustomerReceipt
                ? $"Auto-Posting Penerimaan Pembayaran Pelanggan {payment.PaymentNumber}"
                : $"Auto-Posting Pembayaran Tagihan Vendor {payment.PaymentNumber}",
            createdByUserId: userId);

        if (payment.PaymentType == PaymentType.CustomerReceipt)
        {
            entry.AddDebitLine(cashBankCoaId, roundedTotal, $"Kas/Bank Masuk {payment.PaymentNumber}");
            entry.AddCreditLine(accounts["1-1300"].Id, roundedTotal, $"Pelunasan Piutang Usaha {payment.PaymentNumber}");
        }
        else
        {
            entry.AddDebitLine(accounts["2-1100"].Id, roundedTotal, $"Pelunasan Hutang Usaha {payment.PaymentNumber}");
            entry.AddCreditLine(cashBankCoaId, roundedTotal, $"Kas/Bank Keluar {payment.PaymentNumber}");
        }

        entry.Post(userId);

        context.JournalEntries.Add(entry);
        return Result<Guid>.Success(entry.Id);
    }

    public async Task<Result<Guid>> PostPayrollJournalAsync(
        PayrollPeriod period,
        Guid branchId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var periodCheck = await EnsureFiscalPeriodOpenAsync(period.CompanyId, period.EndDate, cancellationToken);
        if (periodCheck.IsFailure)
            return Result<Guid>.Failure(periodCheck.Error);

        var totalNetSalary = Math.Round(period.Items.Sum(i => i.NetSalary), 2);
        if (totalNetSalary <= 0)
            return Result<Guid>.Success(Guid.Empty);

        var existingId = await FindExistingPostedJournalIdAsync(period.CompanyId, "PAYROLL", period.PeriodName, cancellationToken);
        if (existingId.HasValue)
            return Result<Guid>.Success(existingId.Value);

        var resolvedBranchId = await ResolveBranchIdAsync(period.CompanyId, branchId, userId, cancellationToken);
        var accounts = await EnsureAndGetAccountsAsync(period.CompanyId, userId, cancellationToken);
        var journalNumber = await sequenceService.NextDocumentNumberAsync(period.CompanyId, resolvedBranchId, "JV", cancellationToken);

        var entry = JournalEntry.Create(
            companyId: period.CompanyId,
            branchId: resolvedBranchId,
            journalNumber: journalNumber,
            journalDate: period.EndDate,
            referenceType: "PAYROLL",
            referenceNumber: period.PeriodName,
            description: $"Auto-Posting Pembayaran Gaji Karyawan ({period.PeriodName})",
            createdByUserId: userId);

        entry.AddDebitLine(accounts["6-1100"].Id, totalNetSalary, $"Beban Gaji & Upah ({period.PeriodName})");
        entry.AddCreditLine(accounts["1-1200"].Id, totalNetSalary, $"Pembayaran Gaji via Bank ({period.PeriodName})");
        entry.Post(userId);

        context.JournalEntries.Add(entry);
        return Result<Guid>.Success(entry.Id);
    }

    private async Task<Guid?> FindExistingPostedJournalIdAsync(
        Guid companyId,
        string referenceType,
        string referenceNumber,
        CancellationToken cancellationToken)
    {
        var local = context.JournalEntries.Local
            .FirstOrDefault(j => j.CompanyId == companyId
                && j.ReferenceType == referenceType
                && j.ReferenceNumber == referenceNumber
                && j.Status == JournalStatus.Posted
                && !j.IsDeleted);

        if (local != null)
            return local.Id;

        var existing = await context.JournalEntries
            .AsNoTracking()
            .Where(j => j.CompanyId == companyId
                && j.ReferenceType == referenceType
                && j.ReferenceNumber == referenceNumber
                && j.Status == JournalStatus.Posted
                && !j.IsDeleted)
            .Select(j => (Guid?)j.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return existing;
    }

    private async Task<Dictionary<string, ChartOfAccount>> EnsureAndGetAccountsAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var codes = StandardAccounts.Select(a => a.Code).ToList();

        var dbAccounts = await context.ChartOfAccounts
            .Where(c => c.CompanyId == companyId && codes.Contains(c.AccountCode) && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var dict = dbAccounts.ToDictionary(c => c.AccountCode, StringComparer.OrdinalIgnoreCase);

        foreach (var localAcc in context.ChartOfAccounts.Local.Where(c => c.CompanyId == companyId && !c.IsDeleted))
        {
            dict.TryAdd(localAcc.AccountCode, localAcc);
        }

        foreach (var std in StandardAccounts)
        {
            if (!dict.ContainsKey(std.Code))
            {
                var created = ChartOfAccount.Create(
                    companyId: companyId,
                    accountCode: std.Code,
                    accountName: std.Name,
                    accountType: std.Type,
                    normalBalance: std.Balance,
                    createdByUserId: userId);

                context.ChartOfAccounts.Add(created);
                dict[std.Code] = created;
            }
        }

        return dict;
    }

    private async Task<Guid> ResolveBranchIdAsync(
        Guid companyId,
        Guid preferredBranchId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (preferredBranchId != Guid.Empty)
            return preferredBranchId;

        var localBranch = context.Branches.Local
            .Where(b => b.CompanyId == companyId && !b.IsDeleted)
            .OrderByDescending(b => b.IsHeadOffice)
            .FirstOrDefault();

        if (localBranch != null)
            return localBranch.Id;

        var dbBranchId = await context.Branches
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && !b.IsDeleted)
            .OrderByDescending(b => b.IsHeadOffice)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (dbBranchId != Guid.Empty)
            return dbBranchId;

        var fallbackBranch = Branch.Create(
            companyId: companyId,
            code: "HO-JKT",
            name: "Kantor Pusat",
            address: "Jakarta",
            phone: "021-000000",
            isHeadOffice: true,
            createdByUserId: userId);

        context.Branches.Add(fallbackBranch);
        return fallbackBranch.Id;
    }
}
