namespace ERP.Domain.Entities.Master;

public class DocumentSequence
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int CurrentSequence { get; private set; }

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;

    private DocumentSequence() { }

    public static DocumentSequence Create(Guid companyId, Guid branchId, string documentType, int year, int month)
    {
        return new DocumentSequence
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            DocumentType = documentType.Trim().ToUpperInvariant(),
            Year = year,
            Month = month,
            CurrentSequence = 0
        };
    }

    public int Increment()
    {
        CurrentSequence++;
        return CurrentSequence;
    }
}

