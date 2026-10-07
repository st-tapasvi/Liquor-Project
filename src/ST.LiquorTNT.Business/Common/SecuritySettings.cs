using System.Globalization;

namespace ST.LiquorTNT.Business.Common;

/// <summary>
/// Strongly-typed view of the global SECURITY_CONFIG table. Every value comes from the database,
/// never from a code constant, so the admin panel can change behaviour without a redeploy.
/// A missing, blank or nonsensical value (e.g. 0 attempts, "abc") falls back to a safe default,
/// so a bad edit can never silently disable locking or lock on the first wrong password.
/// </summary>
public sealed class SecuritySettings
{
    // --- failed login / account lock ---
    public bool FailedLoginLockEnabled { get; init; }
    public int MaxFailedLoginAttempts { get; init; }
    public int AccountLockDurationMinutes { get; init; }

    // --- sessions ---
    public bool SessionLimitEnabled { get; init; }
    public int MaxActiveSessions { get; init; }
    public int SessionExpiryMinutes { get; init; }      // hard limit of a session, even while working
    public int SessionIdleMinutes { get; init; }        // sliding window: no API call for this long ends it
    public string SessionFullBehaviour { get; init; } = "REJECT";

    // --- security questions ---
    public bool SecurityQuestionEnabled { get; init; }
    public int SecurityQuestionRequired { get; init; }

    // --- forgot-password reset ---
    public int PasswordResetExpiryMinutes { get; init; }
    public int PasswordResetMaxAttempts { get; init; }

    // --- application log: NORMAL (default) or DETAIL (bodies, method input/output, SQL) ---
    public bool DetailLogging { get; init; }

    /// <summary>
    /// Builds the typed settings from the raw key/value rows of SECURITY_CONFIG.
    /// Kept as a pure function (no database) so it can be unit-tested directly.
    /// </summary>
    public static SecuritySettings FromEntries(IReadOnlyDictionary<string, string> entries)
    {
        return new SecuritySettings
        {
            FailedLoginLockEnabled     = Bool(entries, Keys.FailedLoginLockEnabled, true),
            MaxFailedLoginAttempts     = PositiveInt(entries, Keys.MaxFailedLoginAttempts, 3),
            AccountLockDurationMinutes = PositiveInt(entries, Keys.AccountLockDurationMinutes, 1440),

            SessionLimitEnabled        = Bool(entries, Keys.SessionLimitEnabled, true),
            MaxActiveSessions          = PositiveInt(entries, Keys.MaxActiveSessions, 2),
            SessionExpiryMinutes       = PositiveInt(entries, Keys.SessionExpiryMinutes, 1440),
            SessionIdleMinutes         = PositiveInt(entries, Keys.SessionIdleMinutes, 60),
            SessionFullBehaviour       = Str(entries, Keys.SessionFullBehaviour, "REJECT"),

            SecurityQuestionEnabled    = Bool(entries, Keys.SecurityQuestionEnabled, true),
            SecurityQuestionRequired   = PositiveInt(entries, Keys.SecurityQuestionRequired, 1),

            PasswordResetExpiryMinutes = PositiveInt(entries, Keys.PasswordResetExpiryMinutes, 15),
            PasswordResetMaxAttempts   = PositiveInt(entries, Keys.PasswordResetMaxAttempts, 5),

            DetailLogging              = Str(entries, Keys.LogMode, LogModes.Normal).Equals(LogModes.Detail, StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>The accepted values of <see cref="Keys.LogMode"/>; anything else means NORMAL.</summary>
    public static class LogModes
    {
        public const string Normal = "NORMAL";
        public const string Detail = "DETAIL";
    }

    private static string Str(IReadOnlyDictionary<string, string> e, string key, string fallback)
        => e.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : fallback;

    /// <summary>Counts and durations must be at least 1; anything else is a misconfiguration → default.</summary>
    private static int PositiveInt(IReadOnlyDictionary<string, string> e, string key, int fallback)
        => e.TryGetValue(key, out var v)
           && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
           && n >= 1
               ? n : fallback;

    private static bool Bool(IReadOnlyDictionary<string, string> e, string key, bool fallback)
    {
        if (!e.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v))
        {
            return fallback;
        }

        return v.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" => true,
            "0" or "false" or "no" => false,
            _ => fallback,        // unrecognised text never flips a security switch
        };
    }

    /// <summary>The exact keys stored in SECURITY_CONFIG. One place, reused by the admin module too.</summary>
    public static class Keys
    {
        public const string FailedLoginLockEnabled     = "FAILED_LOGIN_LOCK_ENABLED";
        public const string MaxFailedLoginAttempts     = "MAX_FAILED_LOGIN_ATTEMPTS";
        public const string AccountLockDurationMinutes = "ACCOUNT_LOCK_DURATION_MINUTES";
        public const string SessionLimitEnabled        = "SESSION_LIMIT_ENABLED";
        public const string MaxActiveSessions          = "MAX_ACTIVE_SESSIONS";
        public const string SessionExpiryMinutes       = "SESSION_EXPIRY_MINUTES";
        public const string SessionIdleMinutes         = "SESSION_IDLE_MINUTES";
        public const string SessionFullBehaviour       = "SESSION_FULL_BEHAVIOUR";
        public const string SecurityQuestionEnabled    = "SECURITY_QUESTION_ENABLED";
        public const string SecurityQuestionRequired   = "SECURITY_QUESTION_REQUIRED";
        public const string PasswordResetExpiryMinutes = "PASSWORD_RESET_EXPIRY_MINUTES";
        public const string PasswordResetMaxAttempts   = "PASSWORD_RESET_MAX_ATTEMPTS";
        public const string LogMode                    = "LOG_MODE";
    }
}
