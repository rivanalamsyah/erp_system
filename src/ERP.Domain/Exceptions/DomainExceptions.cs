namespace ERP.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public class UnbalancedJournalDomainException(decimal totalDebit, decimal totalCredit)
    : DomainException($"Jurnal akuntansi tidak seimbang! Total Debit ({totalDebit:N2}) tidak sama dengan Total Kredit ({totalCredit:N2}).");

public class InsufficientStockDomainException(string sku, decimal availableStock, decimal requestedQuantity)
    : DomainException($"Stok barang [{sku}] tidak mencukupi! Tersedia: {availableStock}, Diminta: {requestedQuantity}.");

public class CreditLimitExceededDomainException(string customerName, decimal currentReceivables, decimal orderAmount, decimal creditLimit)
    : DomainException($"Pelanggan [{customerName}] melebihi batas kredit! Piutang berjalan: {currentReceivables:N2}, Pesanan baru: {orderAmount:N2}, Limit: {creditLimit:N2}.");

public class FiscalPeriodClosedDomainException(int year, int month)
    : DomainException($"Periode fiskal {year}-{month:D2} telah ditutup secara permanen. Transaksi tidak dapat diposting.");

