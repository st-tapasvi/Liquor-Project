namespace ST.LiquorTNT.Contracts.Users;

/// <summary>The temporary password an administrator gives a user. The user must replace it at the next login.</summary>
public sealed class ResetUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
