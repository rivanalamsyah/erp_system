namespace ERP.Application.Common.Security;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute(string permissionCode) : Attribute
{
    public string PermissionCode { get; } = permissionCode;
}

