using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ERP.Application.Common.Interfaces;

namespace ERP.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? _circuitPrincipal;
    private Guid? _activeBranchIdOverride;
    private Guid? _activeCompanyIdOverride;
    private bool? _activeIsHeadOfficeOverride;
    private string? _ipAddressOverride;
    private HashSet<string>? _permissionOverrides;

    public event Action? OnContextChanged;

    private ClaimsPrincipal? User =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
            ? httpContextAccessor.HttpContext.User
            : _circuitPrincipal ?? httpContextAccessor.HttpContext?.User;

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
            if (_activeCompanyIdOverride.HasValue && _activeCompanyIdOverride.Value != Guid.Empty)
            {
                return _activeCompanyIdOverride.Value;
            }

            var claim = User?.FindFirst("CompanyId")?.Value;
            return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
        }
    }

    public Guid BranchId
    {
        get
        {
            if (_activeBranchIdOverride.HasValue && _activeBranchIdOverride.Value != Guid.Empty)
            {
                return _activeBranchIdOverride.Value;
            }

            var claim = User?.FindFirst("BranchId")?.Value;
            return Guid.TryParse(claim, out var guid) ? guid : Guid.Empty;
        }
    }

    public bool IsHeadOffice
    {
        get
        {
            if (_activeIsHeadOfficeOverride.HasValue)
            {
                return _activeIsHeadOfficeOverride.Value;
            }

            return User?.FindFirst("IsHeadOffice")?.Value == "true";
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsSuperAdmin => IsAuthenticated && (User?.IsInRole(ERP.Shared.Security.Roles.SuperAdmin) ?? false);

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
        ?? _ipAddressOverride
        ?? User?.FindFirst("IpAddress")?.Value;

    public IReadOnlyList<Guid> AllowedBranchIds
    {
        get
        {
            var claims = User?.FindAll("AllowedBranchId")
                .Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();
            return claims ?? [];
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var roles = User?.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return roles ?? [];
        }
    }

    public bool HasPermission(string permissionCode)
    {
        if (!IsAuthenticated) return false;
        if (IsSuperAdmin) return true;

        if (_permissionOverrides != null)
        {
            return _permissionOverrides.Contains(permissionCode);
        }

        return User?.HasClaim(c =>
            c.Type == "Permission" &&
            c.Value.Equals(permissionCode, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    public bool SetActiveBranch(Guid branchId, Guid? companyId = null, bool? isHeadOffice = null)
    {
        if (branchId == Guid.Empty) return false;

        var allowed = AllowedBranchIds;
        if (!IsSuperAdmin && !IsHeadOffice && allowed.Count > 0 && !allowed.Contains(branchId))
        {
            return false;
        }

        _activeBranchIdOverride = branchId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            _activeCompanyIdOverride = companyId.Value;
        }
        if (isHeadOffice.HasValue)
        {
            _activeIsHeadOfficeOverride = isHeadOffice.Value;
        }

        OnContextChanged?.Invoke();
        return true;
    }

    public void SetCurrentUserContext(ClaimsPrincipal? principal, string? ipAddress = null)
    {
        _circuitPrincipal = principal;
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            _ipAddressOverride = ipAddress;
        }
    }

    public void OverridePermissions(IEnumerable<string>? permissions)
    {
        _permissionOverrides = permissions != null
            ? new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase)
            : null;
        OnContextChanged?.Invoke();
    }
}
