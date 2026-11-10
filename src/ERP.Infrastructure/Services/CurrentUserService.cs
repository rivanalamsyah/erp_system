using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ERP.Application.Common.Interfaces;

namespace ERP.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var guid) ? guid : null;
        }
    }

    public string? UserName => User?.Identity?.Name;

    public string? FullName => User?.FindFirst("FullName")?.Value ?? UserName;

    public Guid CompanyId
    {
        get
        {
            var claim = User?.FindFirst("CompanyId")?.Value;
            return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
        }
    }

    public Guid BranchId
    {
        get
        {
            var claim = User?.FindFirst("BranchId")?.Value;
            return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
        }
    }

    public bool IsHeadOffice => User?.FindFirst("IsHeadOffice")?.Value == "true";

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<Guid> AllowedBranchIds
    {
        get
        {
            var claims = User?.FindAll("AllowedBranchId")
                .Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();
            return claims ?? [];
        }
    }

    public bool HasPermission(string permissionCode)
    {
        if (!IsAuthenticated) return false;
        if (User?.IsInRole("SUPER_ADMIN") == true) return true;

        return User?.HasClaim(c => c.Type == "Permission" && c.Value.Equals(permissionCode, StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}
