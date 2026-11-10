using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Finance;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Finance.JournalEntries;

public record JournalLineInput(
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string? Description = null);

[RequirePermission(Permissions.Finance.JournalCreate)]
public record CreateJournalEntryCommand(
    Guid CompanyId,
    Guid BranchId,
    DateOnly JournalDate,
    string ReferenceType,
    string ReferenceNumber,
    string Description,
    List<JournalLineInput> Lines) : IRequest<Result<Guid>>;

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().WithMessage("Deskripsi jurnal wajib diisi.");
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count >= 2)
            .WithMessage("Jurnal umum wajib memiliki minimal 2 baris entri.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty().WithMessage("Akun wajib dipilih.");
            line.RuleFor(l => l).Must(l => (l.Debit > 0 && l.Credit == 0) || (l.Credit > 0 && l.Debit == 0))
                .WithMessage("Setiap baris jurnal harus memiliki salah satu dari Debit atau Kredit (tidak boleh keduanya atau nol).");
        });
    }
}

public class CreateJournalEntryCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateJournalEntryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        // BR-ACC-002: Cek periode fiskal
        var period = await context.FiscalPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == request.CompanyId
                && p.FiscalYear == request.JournalDate.Year
                && p.PeriodNumber == request.JournalDate.Month, cancellationToken);

        if (period != null && period.IsClosed)
        {
            return Result<Guid>.Failure(Error.Validation(
                "BR-ACC-002",
                $"Transaksi tidak diizinkan: Periode fiskal bulan {request.JournalDate.Month}/{request.JournalDate.Year} telah ditutup (Closed Period)."));
        }

        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "JV",
            cancellationToken);

        var entry = JournalEntry.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            journalNumber: docNumber,
            journalDate: request.JournalDate,
            referenceType: request.ReferenceType,
            referenceNumber: request.ReferenceNumber,
            description: request.Description,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var line in request.Lines)
        {
            if (line.Debit > 0)
                entry.AddDebitLine(line.AccountId, line.Debit, line.Description);
            else if (line.Credit > 0)
                entry.AddCreditLine(line.AccountId, line.Credit, line.Description);
        }

        context.JournalEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entry.Id);
    }
}

[RequirePermission(Permissions.Finance.JournalPost)]
public record PostJournalEntryCommand(Guid Id) : IRequest<Result>;

public class PostJournalEntryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<PostJournalEntryCommand, Result>
{
    public async Task<Result> Handle(
        PostJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await context.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == request.Id, cancellationToken);

        if (entry == null)
            return Result.Failure(Error.NotFound("JV.NotFound", "Data jurnal tidak ditemukan."));

        // BR-ACC-002: Cek apakah periode fiskal ditutup
        var period = await context.FiscalPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == entry.CompanyId
                && p.FiscalYear == entry.JournalDate.Year
                && p.PeriodNumber == entry.JournalDate.Month, cancellationToken);

        if (period != null && period.IsClosed)
        {
            return Result.Failure(Error.Validation(
                "BR-ACC-002",
                $"Posting jurnal ditolak: Periode fiskal {entry.JournalDate.Month}/{entry.JournalDate.Year} telah ditutup."));
        }

        try
        {
            // BR-ACC-001: Zero Imbalance Invariant enforced inside Post()
            entry.Post(currentUserService.UserId ?? Guid.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation("BR-ACC-001", ex.Message));
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

