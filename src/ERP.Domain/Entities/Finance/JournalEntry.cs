using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Finance;

public enum JournalStatus
{
    Draft,
    Posted,
    Reversed
}

public class JournalEntry : BaseEntity<Guid>
{
    public string JournalNumber { get; private set; } = string.Empty;
    public DateOnly JournalDate { get; private set; }
    public string ReferenceType { get; private set; } = "Manual";
    public string ReferenceNumber { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public JournalStatus Status { get; private set; } = JournalStatus.Draft;
    public decimal TotalDebit { get; private set; }
    public decimal TotalCredit { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public ICollection<JournalLine> Lines { get; private set; } = [];

    private JournalEntry() { }

    public static JournalEntry Create(
        Guid companyId,
        Guid branchId,
        string journalNumber,
        DateOnly journalDate,
        string referenceType,
        string referenceNumber,
        string description,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            JournalNumber = journalNumber.Trim(),
            JournalDate = journalDate,
            ReferenceType = referenceType.Trim(),
            ReferenceNumber = referenceNumber.Trim(),
            Description = description.Trim(),
            Status = JournalStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddDebitLine(Guid accountId, decimal debitAmount, string? lineDescription = null)
    {
        if (Status != JournalStatus.Draft)
            throw new InvalidOperationException("Baris jurnal hanya dapat ditambahkan pada status Draft.");
        if (debitAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(debitAmount), "Nilai debit harus lebih besar dari 0.");

        Lines.Add(new JournalLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = Id,
            AccountId = accountId,
            Debit = debitAmount,
            Credit = 0,
            Description = lineDescription?.Trim()
        });

        RecalculateTotals();
    }

    public void AddCreditLine(Guid accountId, decimal creditAmount, string? lineDescription = null)
    {
        if (Status != JournalStatus.Draft)
            throw new InvalidOperationException("Baris jurnal hanya dapat ditambahkan pada status Draft.");
        if (creditAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(creditAmount), "Nilai kredit harus lebih besar dari 0.");

        Lines.Add(new JournalLine
        {
            Id = Guid.NewGuid(),
            JournalEntryId = Id,
            AccountId = accountId,
            Debit = 0,
            Credit = creditAmount,
            Description = lineDescription?.Trim()
        });

        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        TotalDebit = Lines.Sum(l => l.Debit);
        TotalCredit = Lines.Sum(l => l.Credit);
    }

    // BR-ACC-001: Keseimbangan Mutlak Jurnal Akuntansi (Zero Imbalance Invariant)
    public void Post(Guid userId)
    {
        if (Status != JournalStatus.Draft)
            throw new InvalidOperationException("Hanya jurnal berstatus Draft yang dapat diposting.");

        if (Lines.Count < 2)
            throw new InvalidOperationException("Jurnal harus memiliki minimal 2 baris (1 sisi debit dan 1 sisi kredit).");

        RecalculateTotals();

        if (TotalDebit <= 0 || TotalCredit <= 0)
            throw new InvalidOperationException("Total nominal jurnal harus lebih besar dari 0.");

        if (TotalDebit != TotalCredit)
        {
            throw new InvalidOperationException(
                $"BR-ACC-001: Keseimbangan jurnal dilanggar. Total Debit (Rp {TotalDebit:N2}) != Total Kredit (Rp {TotalCredit:N2}). Selisih: Rp {Math.Abs(TotalDebit - TotalCredit):N2}.");
        }

        Status = JournalStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class JournalLine
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }

    public JournalEntry JournalEntry { get; set; } = default!;
    public ChartOfAccount Account { get; set; } = default!;
}
