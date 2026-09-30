namespace ST.LiquorTNT.Contracts.Auth;

/// <summary>Verified by the current password, so it needs no session — usable for a forced first-login change.</summary>
public sealed class ChangePasswordRequest
{
    public string UserName { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
