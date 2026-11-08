namespace ERP.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? FullName { get; }
    Guid CompanyId { get; }
    Guid BranchId { get; }
    bool IsHeadOffice { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<Guid> AllowedBranchIds { get; }
    bool HasPermission(string permissionCode);
}

