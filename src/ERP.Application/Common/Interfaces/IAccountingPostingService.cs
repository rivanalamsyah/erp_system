using ERP.Domain.Entities.Finance;
using ERP.Domain.Entities.Hr;
using ERP.Domain.Entities.Procurement;
using ERP.Domain.Entities.Sales;
using ERP.Shared;

namespace ERP.Application.Common.Interfaces;

/// <summary>
/// Service untuk validasi periode fiskal (BR-ACC-002) dan pembuatan jurnal umum (GL) otomatis
/// yang seimbang (BR-ACC-001) serta idempoten dari transaksi sub-ledger.
/// </summary>
public interface IAccountingPostingService
{
    Task<Result> EnsureFiscalPeriodOpenAsync(Guid companyId, DateOnly transactionDate, CancellationToken cancellationToken = default);

    Task<Result<Guid>> PostGoodsReceiptJournalAsync(GoodsReceipt grn, decimal totalReceiptValue, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> PostVendorBillJournalAsync(VendorBill bill, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> PostDeliveryCogsJournalAsync(Delivery delivery, decimal totalCogsAmount, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> PostSalesInvoiceJournalAsync(SalesInvoice invoice, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> PostPaymentJournalAsync(Payment payment, Guid userId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> PostPayrollJournalAsync(PayrollPeriod period, Guid branchId, Guid userId, CancellationToken cancellationToken = default);
}

