namespace ST.LiquorTNT.Contracts.Auth;

public sealed class SessionResponse
{
    public int Id { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    /// <summary>True for the session that made this request.</summary>
    public bool IsCurrent { get; set; }
}
