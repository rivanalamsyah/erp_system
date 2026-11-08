namespace ERP.Application.Features.Finance.JournalEntries;

public record JournalLineDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? Description);

public record JournalEntryDto(
    Guid Id,
    string JournalNumber,
    DateOnly JournalDate,
    string ReferenceType,
    string ReferenceNumber,
    string Description,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    DateTime? PostedAtUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<JournalLineDto> Lines);
