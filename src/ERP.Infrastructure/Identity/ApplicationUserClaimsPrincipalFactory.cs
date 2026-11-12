using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ERP.Infrastructure.Identity.Models;
using ERP.Infrastructure.Persistence.Context;
using ERP.Shared.Security;

namespace ERP.Infrastructure.Identity;

public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor,
    ApplicationDbContext dbContext)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim("FullName", string.IsNullOrWhiteSpace(user.FullName) ? (user.Email ?? user.UserName ?? "") : user.FullName));

        var roles = await UserManager.GetRolesAsync(user);
        var isSuperAdmin = roles.Contains(Roles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

        // 1. Resolve User Branches & Company context
        var userBranches = await dbContext.UserBranches
            .AsNoTracking()
            .Where(ub => ub.UserId == user.Id)
            .ToListAsync();

        var assignedBranchIds = userBranches.Select(ub => ub.BranchId).Distinct().ToList();

        var branches = await dbContext.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => !b.IsDeleted && (isSuperAdmin || assignedBranchIds.Contains(b.Id)))
            .OrderByDescending(b => b.IsHeadOffice)
            .ThenBy(b => b.Code)
            .ToListAsync();

        if (branches.Count == 0)
        {
            branches = await dbContext.Branches
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.IsHeadOffice)
                .ThenBy(b => b.Code)
                .Take(1)
                .ToListAsync();
        }

        var defaultUserBranchId = userBranches.FirstOrDefault(ub => ub.IsDefault)?.BranchId
            ?? userBranches.FirstOrDefault()?.BranchId;

        var defaultBranch = (defaultUserBranchId.HasValue
                ? branches.FirstOrDefault(b => b.Id == defaultUserBranchId.Value)
                : null)
            ?? branches.FirstOrDefault(b => b.IsHeadOffice)
            ?? branches.FirstOrDefault();

        var companyId = defaultBranch?.CompanyId ?? Guid.Empty;
        if (companyId == Guid.Empty)
        {
            var firstCompany = await dbContext.Companies
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Code)
                .FirstOrDefaultAsync();
            companyId = firstCompany?.Id ?? Guid.Empty;
        }

        var branchId = defaultBranch?.Id ?? Guid.Empty;
        var isHeadOffice = defaultBranch?.IsHeadOffice ?? isSuperAdmin;

        if (companyId != Guid.Empty)
        {
            identity.AddClaim(new Claim("CompanyId", companyId.ToString()));
        }

        if (branchId != Guid.Empty)
        {
            identity.AddClaim(new Claim("BranchId", branchId.ToString()));
        }

        identity.AddClaim(new Claim("IsHeadOffice", isHeadOffice ? "true" : "false"));

        // If user is at HeadOffice or SuperAdmin, allow all active branches within their company
        List<Guid> allowedBranchIds;
        if (isSuperAdmin || isHeadOffice)
        {
            allowedBranchIds = await dbContext.Branches
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => !b.IsDeleted && (isSuperAdmin || b.CompanyId == companyId))
                .Select(b => b.Id)
                .ToListAsync();
        }
        else
        {
            allowedBranchIds = branches.Select(b => b.Id).Distinct().ToList();
        }

        foreach (var allowedId in allowedBranchIds)
        {
            identity.AddClaim(new Claim("AllowedBranchId", allowedId.ToString()));
        }

        // 2. Hydrate PBAC Permissions from RolePermissions table
        HashSet<string> permissionCodes;
        if (isSuperAdmin)
        {
            permissionCodes = Permissions.All.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else if (roles.Count > 0)
        {
            var normalizedRoles = roles.Select(r => r.ToUpperInvariant()).ToList();
            var roleIds = await dbContext.Roles
                .AsNoTracking()
                .Where(r => r.NormalizedName != null && normalizedRoles.Contains(r.NormalizedName))
                .Select(r => r.Id)
                .ToListAsync();

            var dbPermissions = await dbContext.RolePermissions
                .AsNoTracking()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToListAsync();

            permissionCodes = dbPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            permissionCodes = [];
        }

        foreach (var code in permissionCodes)
        {
            identity.AddClaim(new Claim("Permission", code));
        }

        return identity;
    }
}

