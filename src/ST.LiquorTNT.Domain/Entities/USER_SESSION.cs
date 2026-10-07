namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>USER_SESSION</c>: one login session. Only the hash of the bearer token is stored.
/// Two clocks decide how long it lives:
/// <list type="bullet">
/// <item><see cref="ExpiresAt"/> — the sliding deadline: last activity + the idle window. Activity moves it forward.</item>
/// <item><see cref="AbsoluteExpiresAt"/> — the hard limit set at login. Activity never moves it; <see cref="ExpiresAt"/> never passes it.</item>
/// </list>
/// A session is usable while <see cref="Status"/> is ACTIVE and <see cref="ExpiresAt"/> is in the future;
/// logout, admin revocation and expiry each end it with a distinct status for the audit trail.
/// <para>
/// <see cref="ActiveSupplierCodeId"/> is the supplier code the user picked after login (switchable). Rights and data of
/// every call are scoped to it; it is kept here, on the server, and never taken from the request.
/// </para>
/// </summary>
public class USER_SESSION
{
    public const string StatusActive = "ACTIVE";
    public const string StatusExpired = "EXPIRED";
    public const string StatusRevoked = "REVOKED";
    public const string StatusLoggedOut = "LOGGED_OUT";

    private USER_SESSION()
    {
        SessionTokenHash = string.Empty;
        Status = StatusActive;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int? ActiveSupplierCodeId { get; private set; }
    public string SessionTokenHash { get; private set; }
    public DateTime LoginAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime AbsoluteExpiresAt { get; private set; }
    public DateTime? LogoutAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static USER_SESSION Create(
        int userId, string tokenHash, DateTime now, int idleMinutes, DateTime absoluteExpiresAt, string? ipAddress, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        if (idleMinutes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(idleMinutes), "The idle window must be at least one minute.");
        }

        if (absoluteExpiresAt <= now)
        {
            throw new ArgumentException("The session limit must be after the login time.", nameof(absoluteExpiresAt));
        }

        var session = new USER_SESSION
        {
            UserId = userId,
            SessionTokenHash = tokenHash,
            LoginAt = now,
            AbsoluteExpiresAt = absoluteExpiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Status = StatusActive,
            CreatedAt = now,
        };

        session.Slide(now, idleMinutes);
        return session;
    }

    public bool IsActiveAt(DateTime now) => Status == StatusActive && ExpiresAt > now;

    /// <summary>True when the hard limit is the reason the session is over (as opposed to being idle too long).</summary>
    public bool ReachedLimitAt(DateTime now) => AbsoluteExpiresAt <= now;

    /// <summary>Activity: the idle window starts again from <paramref name="now"/>, but never beyond the hard limit.</summary>
    public void Slide(DateTime now, int idleMinutes)
    {
        LastActivityAt = now;
        var idleDeadline = now.AddMinutes(idleMinutes);
        ExpiresAt = idleDeadline < AbsoluteExpiresAt ? idleDeadline : AbsoluteExpiresAt;
    }

    /// <summary>The user picked (or switched to) a supplier code. The caller has already checked the user really holds it.</summary>
    public void SelectSupplierCode(int supplierCodeId) => ActiveSupplierCodeId = supplierCodeId;

    public void Logout(DateTime now) => End(StatusLoggedOut, now);

    public void Revoke(DateTime now) => End(StatusRevoked, now);

    public void Expire() => Status = StatusExpired;

    private void End(string status, DateTime now)
    {
        Status = status;
        LogoutAt = now;
    }
}
