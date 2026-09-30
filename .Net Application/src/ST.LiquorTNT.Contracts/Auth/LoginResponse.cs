namespace ST.LiquorTNT.Contracts.Auth;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Hard limit of this session (IST), even while the user is working. After it every call answers
    /// 401 SESSION_EXPIRED: show the password popup, log in again and retry the call.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Minutes without any API call after which the session ends (401 SESSION_TIMED_OUT → login page).</summary>
    public int IdleTimeoutMinutes { get; set; }

    public CurrentUserResponse User { get; set; } = new();
}
