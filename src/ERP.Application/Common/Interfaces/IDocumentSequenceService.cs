namespace ERP.Application.Common.Interfaces;

public interface IDocumentSequenceService
{
    Task<string> NextDocumentNumberAsync(
        Guid companyId,
        Guid branchId,
        string documentType,
        CancellationToken cancellationToken = default);
}

