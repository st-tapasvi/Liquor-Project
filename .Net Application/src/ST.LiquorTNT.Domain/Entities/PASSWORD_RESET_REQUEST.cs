namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>PASSWORD_RESET_REQUEST</c>: one forgot-password attempt. The client holds a random
/// token (only its hash is stored); the request must be VERIFIED by the security answer before the
/// password can be reset, expires after a configured time and allows a limited number of answers.
/// PENDING → VERIFIED → USED, or → EXPIRED / FAILED.
/// </summary>
public class PASSWORD_RESET_REQUEST
{
    public const string StatusPending = "PENDING";
    public const string StatusVerified = "VERIFIED";
    public const string StatusUsed = "USED";
    public const string StatusExpired = "EXPIRED";
    public const string StatusFailed = "FAILED";

    private PASSWORD_RESET_REQUEST()
    {
        RequestTokenHash = string.Empty;
        Status = StatusPending;
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string RequestTokenHash { get; private set; }
    public string Status { get; private set; }
    public int VerifyAttempts { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static PASSWORD_RESET_REQUEST Create(int userId, string tokenHash, DateTime now, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        if (expiresAt <= now)
        {
            throw new ArgumentException("Expiry must be in the future.", nameof(expiresAt));
        }

        return new PASSWORD_RESET_REQUEST
        {
            UserId = userId,
            RequestTokenHash = tokenHash,
            Status = StatusPending,
            VerifyAttempts = 0,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public bool IsOpen => Status is StatusPending or StatusVerified;

    public bool IsExpiredAt(DateTime now) => ExpiresAt <= now;

    /// <summary>A wrong answer. Returns true when the allowed attempts are used up (request becomes FAILED).</summary>
    public bool RegisterFailedVerify(DateTime now, int maxAttempts)
    {
        VerifyAttempts++;
        UpdatedAt = now;

        if (VerifyAttempts >= maxAttempts)
        {
            Status = StatusFailed;
            return true;
        }

        return false;
    }

    public void MarkVerified(DateTime now) => Set(StatusVerified, now);

    public void MarkUsed(DateTime now) => Set(StatusUsed, now);

    public void MarkExpired(DateTime now) => Set(StatusExpired, now);

    private void Set(string status, DateTime now)
    {
        Status = status;
        UpdatedAt = now;
    }
}
