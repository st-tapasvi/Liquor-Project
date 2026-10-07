namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>PASSWORD_POLICY</c>: the password rules for one policy (EASY / MEDIUM / HARD ...).
/// The name carries no behaviour — every rule is a value on this row, edited from the admin panel.
/// </summary>
public class PASSWORD_POLICY
{
    private PASSWORD_POLICY()
    {
        PolicyName = string.Empty;
    }

    public int Id { get; private set; }
    public string PolicyName { get; private set; }
    public int MinLength { get; private set; }
    public int MaxLength { get; private set; }
    public bool RequireUppercase { get; private set; }
    public bool RequireLowercase { get; private set; }
    public bool RequireNumber { get; private set; }
    public bool RequireSpecialCharacter { get; private set; }
    public int PasswordHistoryCount { get; private set; }
    public bool PasswordExpiryEnabled { get; private set; }
    public int? PasswordExpiryDays { get; private set; }
    public bool AllowUsernameInPassword { get; private set; }
    public bool AllowCommonPassword { get; private set; }
    public bool Status { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static PASSWORD_POLICY Create(
        string policyName,
        int minLength,
        int maxLength,
        bool requireUppercase,
        bool requireLowercase,
        bool requireNumber,
        bool requireSpecialCharacter,
        int passwordHistoryCount,
        bool passwordExpiryEnabled,
        int? passwordExpiryDays,
        bool allowUsernameInPassword,
        bool allowCommonPassword,
        DateTime now,
        int? createdBy)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            throw new ArgumentException("Policy name is required.", nameof(policyName));
        }

        var policy = new PASSWORD_POLICY
        {
            PolicyName = policyName.Trim(),
            Status = true,
            CreatedAt = now,
            CreatedBy = createdBy,
        };

        policy.UpdateRules(minLength, maxLength, requireUppercase, requireLowercase, requireNumber,
            requireSpecialCharacter, passwordHistoryCount, passwordExpiryEnabled, passwordExpiryDays,
            allowUsernameInPassword, allowCommonPassword, now, createdBy);

        return policy;
    }

    public void UpdateRules(
        int minLength,
        int maxLength,
        bool requireUppercase,
        bool requireLowercase,
        bool requireNumber,
        bool requireSpecialCharacter,
        int passwordHistoryCount,
        bool passwordExpiryEnabled,
        int? passwordExpiryDays,
        bool allowUsernameInPassword,
        bool allowCommonPassword,
        DateTime now,
        int? updatedBy)
    {
        if (minLength < 1 || maxLength < minLength)
        {
            throw new ArgumentException("Minimum length must be at least 1 and not exceed the maximum length.");
        }

        if (passwordHistoryCount < 0)
        {
            throw new ArgumentException("Password history count cannot be negative.", nameof(passwordHistoryCount));
        }

        if (passwordExpiryEnabled && (passwordExpiryDays is null || passwordExpiryDays < 1))
        {
            throw new ArgumentException("Expiry days must be at least 1 when expiry is enabled.", nameof(passwordExpiryDays));
        }

        MinLength = minLength;
        MaxLength = maxLength;
        RequireUppercase = requireUppercase;
        RequireLowercase = requireLowercase;
        RequireNumber = requireNumber;
        RequireSpecialCharacter = requireSpecialCharacter;
        PasswordHistoryCount = passwordHistoryCount;
        PasswordExpiryEnabled = passwordExpiryEnabled;
        PasswordExpiryDays = passwordExpiryEnabled ? passwordExpiryDays : null;
        AllowUsernameInPassword = allowUsernameInPassword;
        AllowCommonPassword = allowCommonPassword;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// One policy built from several: a user with many roles gets the STRICTEST value of every rule across
    /// the policies of those roles (owner decision). The result is only used in memory, never saved.
    /// </summary>
    public static PASSWORD_POLICY Strictest(IReadOnlyCollection<PASSWORD_POLICY> policies, DateTime now)
    {
        if (policies.Count == 0)
        {
            throw new ArgumentException("At least one policy is needed.", nameof(policies));
        }

        if (policies.Count == 1)
        {
            return policies.First();
        }

        var minLength = policies.Max(p => p.MinLength);                      // longest minimum
        var maxLength = Math.Max(minLength, policies.Min(p => p.MaxLength)); // shortest maximum, never below the minimum
        var expiring = policies.Where(p => p.PasswordExpiryEnabled && p.PasswordExpiryDays is > 0).ToList();

        return Create(
            "STRICTEST(" + string.Join(",", policies.Select(p => p.PolicyName)) + ")",
            minLength,
            maxLength,
            requireUppercase: policies.Any(p => p.RequireUppercase),
            requireLowercase: policies.Any(p => p.RequireLowercase),
            requireNumber: policies.Any(p => p.RequireNumber),
            requireSpecialCharacter: policies.Any(p => p.RequireSpecialCharacter),
            passwordHistoryCount: policies.Max(p => p.PasswordHistoryCount),
            passwordExpiryEnabled: expiring.Count > 0,
            passwordExpiryDays: expiring.Count > 0 ? expiring.Min(p => p.PasswordExpiryDays) : null,   // soonest expiry
            allowUsernameInPassword: policies.All(p => p.AllowUsernameInPassword),
            allowCommonPassword: policies.All(p => p.AllowCommonPassword),
            now,
            createdBy: null);
    }

    /// <summary>When a password set at <paramref name="now"/> expires under this policy; null = never.</summary>
    public DateTime? ExpiryFrom(DateTime now) =>
        PasswordExpiryEnabled && PasswordExpiryDays is > 0 ? now.AddDays(PasswordExpiryDays.Value) : null;
}
