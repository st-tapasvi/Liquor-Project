namespace ST.LiquorTNT.Contracts.Auth;

public sealed class SecurityQuestionResponse
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
}

/// <summary>Sets the caller's security question. The current password proves it is really them.</summary>
public sealed class SetSecurityQuestionRequest
{
    public int QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordStartRequest
{
    public string UserName { get; set; } = string.Empty;
}

public sealed class ForgotPasswordStartResponse
{
    /// <summary>Opaque handle for the next two steps. Keep it client-side only.</summary>
    public string RequestToken { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>IST.</summary>
    public DateTime ExpiresAt { get; set; }
}

public sealed class ForgotPasswordVerifyRequest
{
    public string RequestToken { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public sealed class ForgotPasswordResetRequest
{
    public string RequestToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
