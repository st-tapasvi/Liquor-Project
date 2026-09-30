using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// Checks a candidate password against a <see cref="PASSWORD_POLICY"/> row. Every rule is driven by
/// the policy's values — there is no EASY/MEDIUM/HARD logic in code. Returns the list of violations
/// (empty = acceptable) so the caller can report them all at once on the "password" field.
/// </summary>
public sealed class PasswordPolicyValidator
{
    // A short list of the most predictable passwords. Reference data, not a rule: the policy decides
    // whether it applies (ALLOW_COMMON_PASSWORD).
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password1", "password123", "passw0rd", "p@ssw0rd",
        "123456", "1234567", "12345678", "123456789", "1234567890", "111111", "123123",
        "qwerty", "qwerty123", "abc123", "abcd1234",
        "admin", "admin123", "admin@123", "administrator",
        "welcome", "welcome1", "welcome@123", "letmein", "iloveyou",
        "user123", "test123", "india123", "pass@123",
    };

    private readonly IPasswordHasher _hasher;

    public PasswordPolicyValidator(IPasswordHasher hasher) => _hasher = hasher;

    public IReadOnlyList<string> Validate(
        string password,
        string? userName,
        PASSWORD_POLICY policy,
        IReadOnlyCollection<string> recentPasswordHashes)
    {
        var errors = new List<string>();

        if (password.Length < policy.MinLength)
        {
            errors.Add($"Password must be at least {policy.MinLength} characters.");
        }

        if (password.Length > policy.MaxLength)
        {
            errors.Add($"Password must be at most {policy.MaxLength} characters.");
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain an uppercase letter.");
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain a lowercase letter.");
        }

        if (policy.RequireNumber && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain a number.");
        }

        if (policy.RequireSpecialCharacter && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain a special character.");
        }

        if (!policy.AllowUsernameInPassword
            && !string.IsNullOrWhiteSpace(userName)
            && password.Contains(userName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Password must not contain the user name.");
        }

        if (!policy.AllowCommonPassword && CommonPasswords.Contains(password))
        {
            errors.Add("Password is too common; choose a less predictable one.");
        }

        if (recentPasswordHashes.Any(hash => _hasher.Verify(password, hash)))
        {
            errors.Add($"Password must differ from the last {policy.PasswordHistoryCount} passwords.");
        }

        return errors;
    }
}
