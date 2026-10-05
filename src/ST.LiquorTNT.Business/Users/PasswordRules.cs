using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// The one place that answers "is this password acceptable for this user?" — used by create user,
/// change password and forgot-password reset, so the rule is applied identically everywhere:
/// resolve the policy through the user's role, then check the policy plus password history.
/// </summary>
public sealed class PasswordRules
{
    private readonly IPasswordPolicyRepository _policies;
    private readonly IUserRepository _users;
    private readonly PasswordPolicyValidator _validator;

    public PasswordRules(IPasswordPolicyRepository policies, IUserRepository users, PasswordPolicyValidator validator)
    {
        _policies = policies;
        _users = users;
        _validator = validator;
    }

    /// <summary>The policy assigned to the role. Every role must have one; a missing mapping is a configuration error.</summary>
    public async Task<PASSWORD_POLICY> RequirePolicyAsync(int? roleId, CancellationToken ct)
    {
        var policy = roleId.HasValue ? await _policies.GetForRoleAsync(roleId.Value, ct) : null;

        return policy ?? throw new BusinessException(
            ErrorCodes.PasswordPolicyNotConfigured,
            "No password policy is assigned to this role.",
            "Assign a password policy to the role in the admin panel before creating or updating passwords.");
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
        var policy = await RequirePolicyAsync(user.RoleId, ct);
        var recent = await _users.GetRecentPasswordHashesAsync(user.Id, policy.PasswordHistoryCount, ct);

        EnsureAcceptable(newPassword, user.UserName, policy, recent);
        return policy;
    }
}
