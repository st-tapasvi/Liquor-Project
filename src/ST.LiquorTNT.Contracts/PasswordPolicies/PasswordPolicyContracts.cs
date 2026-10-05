namespace ST.LiquorTNT.Contracts.PasswordPolicies;

public sealed class PasswordPolicyResponse
{
    public int Id { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public int MinLength { get; set; }
    public int MaxLength { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireNumber { get; set; }
    public bool RequireSpecialCharacter { get; set; }
    public int PasswordHistoryCount { get; set; }
    public bool PasswordExpiryEnabled { get; set; }
    public int? PasswordExpiryDays { get; set; }
    public bool AllowUsernameInPassword { get; set; }
    public bool AllowCommonPassword { get; set; }
    public bool Status { get; set; }

    /// <summary>IST.</summary>
    public DateTime UpdatedAt { get; set; }
}

public sealed class UpdatePasswordPolicyRequest
{
    public int MinLength { get; set; }
    public int MaxLength { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireNumber { get; set; }
    public bool RequireSpecialCharacter { get; set; }
    public int PasswordHistoryCount { get; set; }
    public bool PasswordExpiryEnabled { get; set; }
    public int? PasswordExpiryDays { get; set; }
    public bool AllowUsernameInPassword { get; set; }
    public bool AllowCommonPassword { get; set; }
}
