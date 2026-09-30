using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using ST.LiquorTNT.Business.Common.Abstractions;

namespace ST.LiquorTNT.Api.Security;

/// <summary>
/// Interim gate for user-management and security-administration endpoints until the roles module
/// ships <c>[HasPermission]</c>: the caller's <c>role_id</c> claim must equal
/// <c>SECURITY_CONFIG.ADMIN_ROLE_ID</c>. Data-driven — no role name or id in code.
/// </summary>
public sealed class AdministratorRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Administrator";
}

public sealed class AdministratorHandler : AuthorizationHandler<AdministratorRequirement>
{
    private readonly ISecurityConfigProvider _config;

    public AdministratorHandler(ISecurityConfigProvider config) => _config = config;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorRequirement requirement)
    {
        var settings = await _config.GetAsync(CancellationToken.None);

        if (int.TryParse(context.User.FindFirst("role_id")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var roleId)
            && roleId == settings.AdminRoleId)
        {
            context.Succeed(requirement);
        }
    }
}
