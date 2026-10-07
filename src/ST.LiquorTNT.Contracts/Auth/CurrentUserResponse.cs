namespace ST.LiquorTNT.Contracts.Auth;

public sealed class CurrentUserResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? CompanyId { get; set; }
    public bool ForcePasswordChange { get; set; }
    public DateTime? PasswordExpiresAt { get; set; }
}
