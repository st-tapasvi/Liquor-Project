namespace ST.LiquorTNT.Contracts.Auth;

public sealed class CurrentUserResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? CompanyId { get; set; }
    public bool ForcePasswordChange { get; set; }
    public DateTime? PasswordExpiresAt { get; set; }

    /// <summary>
    /// True → the user has not chosen a security question yet. Show the security-question screen
    /// (PUT /api/securityquestions/mine) first; every other API answers 403 SECURITY_QUESTION_REQUIRED until then.
    /// </summary>
    public bool SecurityQuestionRequired { get; set; }
}
