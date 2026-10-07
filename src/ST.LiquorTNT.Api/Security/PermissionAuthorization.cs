using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ST.LiquorTNT.Business.Access;

namespace ST.LiquorTNT.Api.Security;

/// <summary>"The caller must hold this permission key" - one requirement per <see cref="HasPermissionAttribute"/>.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionKey) => PermissionKey = permissionKey;

    public string PermissionKey { get; }
}

/// <summary>
/// Builds the policy for a "perm:..." name on the fly, so no policy has to be registered per key.
/// Any other policy name goes to the default provider.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[HasPermissionAttribute.PolicyPrefix.Length..]))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}

/// <summary>
/// Checks the permission through <see cref="CurrentAccess"/>, the same code the services use. A refusal is thrown
/// as an AppException (SUPPLIER_CODE_NOT_SELECTED / PERMISSION_DENIED) so ExceptionMiddleware answers with a clear
/// errorCode and the missing key - not a bare 403.
/// </summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly CurrentAccess _access;
    private readonly IHttpContextAccessor _http;

    public PermissionHandler(CurrentAccess access, IHttpContextAccessor http)
    {
        _access = access;
        _http = http;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Not logged in: do nothing, so the normal 401 challenge (UNAUTHENTICATED) answers.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        await _access.EnsurePermissionAsync(requirement.PermissionKey, _http.HttpContext?.RequestAborted ?? CancellationToken.None);
        context.Succeed(requirement);
    }
}
