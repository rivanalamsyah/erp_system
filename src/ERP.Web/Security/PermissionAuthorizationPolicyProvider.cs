using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ERP.Application.Common.Interfaces;
using ERP.Shared.Security;

namespace ERP.Web.Security;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(Roles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(c =>
                c.Type == "Permission" &&
                c.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        using var scope = serviceProvider.CreateScope();
        var currentUserService = scope.ServiceProvider.GetService<ICurrentUserService>();
        if (currentUserService != null && currentUserService.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existingPolicy = await base.GetPolicyAsync(policyName);
        if (existingPolicy != null)
        {
            return existingPolicy;
        }

        var policyBuilder = new AuthorizationPolicyBuilder();
        policyBuilder.RequireAuthenticatedUser();
        policyBuilder.AddRequirements(new PermissionRequirement(policyName));
        return policyBuilder.Build();
    }
}

