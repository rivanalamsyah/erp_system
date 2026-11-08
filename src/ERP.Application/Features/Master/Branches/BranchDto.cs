namespace ERP.Application.Features.Master.Branches;

public record BranchDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    bool IsHeadOffice,
    DateTime CreatedAtUtc);

