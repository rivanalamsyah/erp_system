using System.Security.Claims;

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
    bool IsSuperAdmin { get; }
    string? IpAddress { get; }
    IReadOnlyList<Guid> AllowedBranchIds { get; }
    IReadOnlyList<string> Roles { get; }
    bool HasPermission(string permissionCode);
    bool SetActiveBranch(Guid branchId, Guid? companyId = null, bool? isHeadOffice = null);
    void SetCurrentUserContext(ClaimsPrincipal? principal, string? ipAddress = null);
    void OverridePermissions(IEnumerable<string>? permissions);
    event Action? OnContextChanged;
}
