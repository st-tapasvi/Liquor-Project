using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// The one place that answers "is this password acceptable for this user?" — used by create user,
/// change password and forgot-password reset, so the rule is applied identically everywhere:
/// resolve the policy through the user's roles, then check the policy plus password history.
/// A user with several roles follows the STRICTEST combination of their roles' policies.
/// </summary>
public sealed class PasswordRules
{
    private readonly IPasswordPolicyRepository _policies;
    private readonly IUserRepository _users;
    private readonly PasswordPolicyValidator _validator;
    private readonly IClock _clock;

    public PasswordRules(IPasswordPolicyRepository policies, IUserRepository users, PasswordPolicyValidator validator, IClock clock)
    {
        _policies = policies;
        _users = users;
        _validator = validator;
        _clock = clock;
    }

    /// <summary>
    /// The policy for a user holding <paramref name="roleIds"/>. Every role must have a policy; a missing
    /// mapping is a configuration error. Several roles → the strictest value of each rule.
    /// </summary>
    public async Task<PASSWORD_POLICY> RequirePolicyAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct)
    {
        var distinct = roleIds.Distinct().ToList();
        var byRole = distinct.Count == 0
            ? new Dictionary<int, PASSWORD_POLICY>()
            : await _policies.GetForRolesAsync(distinct, ct);

        if (distinct.Count == 0 || distinct.Any(id => !byRole.ContainsKey(id)))
        {
            throw new BusinessException(
                ErrorCodes.PasswordPolicyNotConfigured,
                "No password policy is assigned to this role.",
                "Assign a password policy to every role of the user in the admin panel before creating or updating passwords.");
        }

        return PASSWORD_POLICY.Strictest(byRole.Values.ToList(), _clock.IndiaNow);
    }

    /// <summary>Throws a 400 on the "password" field listing every violated rule.</summary>
    public void EnsureAcceptable(string password, string? userName, PASSWORD_POLICY policy, IReadOnlyCollection<string> recentPasswordHashes)
    {
        var errors = _validator.Validate(password, userName, policy, recentPasswordHashes);

        if (errors.Count > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["password"] = errors.ToArray() });
        }
    }

    /// <summary>For an existing user: resolves the policy, loads the recent hashes and checks. Returns the policy so the caller can set expiry.</summary>
    public async Task<PASSWORD_POLICY> EnsureAcceptableForUserAsync(USERS user, string newPassword, CancellationToken ct)
    {
        var policy = await RequirePolicyAsync(await _policies.GetRoleIdsForUserAsync(user.Id, ct), ct);
        var recent = await _users.GetRecentPasswordHashesAsync(user.Id, policy.PasswordHistoryCount, ct);

        EnsureAcceptable(newPassword, user.UserName, policy, recent);
        return policy;
    }
}
