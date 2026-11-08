using Microsoft.AspNetCore.Identity;

namespace ERP.Infrastructure.Identity.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<UserBranch> UserBranches { get; set; } = [];
    public ICollection<UserSession> UserSessions { get; set; } = [];
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty; // e.g. 'sales.order.approve'
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationRole Role { get; set; } = default!;
    public Permission Permission { get; set; } = default!;
}

public class UserBranch
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public bool IsDefault { get; set; }

    public ApplicationUser User { get; set; } = default!;
}

public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime LoginTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
    public bool IsExpired { get; set; }

    public ApplicationUser User { get; set; } = default!;
}

