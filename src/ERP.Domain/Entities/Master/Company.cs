using ERP.Domain.Common;

namespace ERP.Domain.Entities.Master;

public class Company : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string TaxIdentificationNumber { get; private set; } = string.Empty; // NPWP
    public string Address { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string CurrencyCode { get; private set; } = "IDR";

    // Persistent Company Business & Policy Settings
    public decimal DefaultVatRate { get; private set; } = 11.0m;
    public int FiscalStartMonth { get; private set; } = 1;
    public int DefaultPaymentTermsDays { get; private set; } = 30;
    public decimal DefaultCreditLimit { get; private set; } = 50000000m;
    public bool EnforceStrictCreditLimit { get; private set; } = true;
    public bool AlertLowStock { get; private set; } = true;
    public int StockWarningThresholdPercentage { get; private set; } = 20;
    public bool AlertOverdueInvoices { get; private set; } = true;
    public int OverdueReminderDays { get; private set; } = 7;

    public ICollection<Branch> Branches { get; private set; } = [];
    public ICollection<Department> Departments { get; private set; } = [];

    private Company() { }

    public static Company Create(
        string code,
        string name,
        string taxIdentificationNumber,
        string address,
        string? phone,
        string? email,
        Guid createdByUserId,
        string currencyCode = "IDR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var id = Guid.NewGuid();
        return new Company
        {
            Id = id,
            CompanyId = id,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            TaxIdentificationNumber = taxIdentificationNumber.Trim(),
            Address = address.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            CurrencyCode = currencyCode.ToUpperInvariant(),
            DefaultVatRate = 11.0m,
            FiscalStartMonth = 1,
            DefaultPaymentTermsDays = 30,
            DefaultCreditLimit = 50000000m,
            EnforceStrictCreditLimit = true,
            AlertLowStock = true,
            StockWarningThresholdPercentage = 20,
            AlertOverdueInvoices = true,
            OverdueReminderDays = 7,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(
        string name,
        string taxIdentificationNumber,
        string address,
        string? phone,
        string? email,
        Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        TaxIdentificationNumber = taxIdentificationNumber.Trim();
        Address = address.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateBusinessSettings(
        decimal defaultVatRate,
        int fiscalStartMonth,
        int defaultPaymentTermsDays,
        decimal defaultCreditLimit,
        bool enforceStrictCreditLimit,
        bool alertLowStock,
        int stockWarningThresholdPercentage,
        bool alertOverdueInvoices,
        int overdueReminderDays,
        Guid updatedByUserId)
    {
        if (defaultVatRate is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(defaultVatRate), "Tarif PPN harus berada di antara 0% dan 100%.");
        if (fiscalStartMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(fiscalStartMonth), "Bulan awal tahun fiskal harus antara 1 dan 12.");
        if (defaultPaymentTermsDays < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultPaymentTermsDays), "Termin pembayaran tidak boleh negatif.");
        if (defaultCreditLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(defaultCreditLimit), "Pagu kredit tidak boleh negatif.");

        DefaultVatRate = defaultVatRate;
        FiscalStartMonth = fiscalStartMonth;
        DefaultPaymentTermsDays = defaultPaymentTermsDays;
        DefaultCreditLimit = defaultCreditLimit;
        EnforceStrictCreditLimit = enforceStrictCreditLimit;
        AlertLowStock = alertLowStock;
        StockWarningThresholdPercentage = Math.Clamp(stockWarningThresholdPercentage, 1, 100);
        AlertOverdueInvoices = alertOverdueInvoices;
        OverdueReminderDays = Math.Max(1, overdueReminderDays);
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
