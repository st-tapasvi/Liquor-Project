using Microsoft.AspNetCore.Authorization;

namespace ST.LiquorTNT.Api.Security;

/// <summary>
/// Puts an endpoint behind one permission key: <c>[HasPermission(Permissions.UserAdd)]</c>.
/// The key becomes a policy name ("perm:user.add") that <see cref="PermissionPolicyProvider"/> turns into a
/// <see cref="PermissionRequirement"/>, checked by <see cref="PermissionHandler"/> against the caller's roles and
/// custom rights in the selected supplier code. Every endpoint carries one, except the auth flow and /health.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(string permissionKey) : base(PolicyPrefix + permissionKey)
    {
    }
}
