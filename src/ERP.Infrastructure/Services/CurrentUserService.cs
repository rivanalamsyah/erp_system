using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ERP.Application.Common.Interfaces;

namespace ERP.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var guid) ? guid : null;
        }
    }

    public string? UserName => _user?.Identity?.Name;

    public string? FullName => _user?.FindFirst("FullName")?.Value ?? UserName;

    public Guid CompanyId
    {
        get
        {
            var claim = _user?.FindFirst("CompanyId")?.Value;
            return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
        }
    }

    public Guid BranchId
    {
        get
        {
            var claim = _user?.FindFirst("BranchId")?.Value;
            return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
        }
    }

    public bool IsHeadOffice => _user?.FindFirst("IsHeadOffice")?.Value == "true";

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<Guid> AllowedBranchIds
    {
        get
        {
            var claims = _user?.FindAll("AllowedBranchId").Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
            return claims ?? [];
        }
    }

    public bool HasPermission(string permissionCode)
    {
        if (!IsAuthenticated) return false;
        if (_user?.IsInRole("SUPER_ADMIN") == true) return true;

        return _user?.HasClaim(c => c.Type == "Permission" && c.Value.Equals(permissionCode, StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}

