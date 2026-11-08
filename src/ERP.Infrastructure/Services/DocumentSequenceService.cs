using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Master;
using ERP.Infrastructure.Persistence.Context;

namespace ERP.Infrastructure.Services;

public class DocumentSequenceService(
    ApplicationDbContext context,
    IDateTimeProvider dateTimeProvider) : IDocumentSequenceService
{
    public async Task<string> NextDocumentNumberAsync(
        Guid companyId,
        Guid branchId,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;
        var docType = documentType.Trim().ToUpperInvariant();

        // 1. Dapatkan kode cabang
        var branch = await context.Branches
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken);

        var branchCode = branch?.Code ?? "HQ";

        // 2. Cari atau buat entri sekuens dokumen bulanan
        var sequence = await context.DocumentSequences
            .FirstOrDefaultAsync(s =>
                s.CompanyId == companyId &&
                s.BranchId == branchId &&
                s.DocumentType == docType &&
                s.Year == now.Year &&
                s.Month == now.Month, cancellationToken);

        if (sequence == null)
        {
            sequence = DocumentSequence.Create(companyId, branchId, docType, now.Year, now.Month);
            context.DocumentSequences.Add(sequence);
        }

        var nextSeq = sequence.Increment();
        await context.SaveChangesAsync(cancellationToken);

        // Format standar enterprise: DOC/BRANCH/YYYYMM/00001
        return $"{docType}/{branchCode}/{now.Year:D4}{now.Month:D2}/{nextSeq:D5}";
    }
}

