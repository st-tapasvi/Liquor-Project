namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for the <c>USERS</c> table — the User module's core entity (entity name = table name).
/// Owns every rule that is true for a user regardless of who calls it: failed-login counting,
/// temporary locking, password state, activation. Limits (max attempts, lock minutes) are passed
/// in as plain values by the caller, which reads them from configuration — nothing is hard-coded.
/// Setters are private: state changes only through the methods below.
/// </summary>
public class USERS
{
    private readonly List<USER_PASSWORD_HISTORY> _passwordHistory = new();

    private USERS()
    {
        UserName = string.Empty;
    }

    public int Id { get; private set; }

    // identity / profile
    public string UserName { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? EmployeeCode { get; private set; }

    // access assignment (role-wise password policy uses RoleId; company is the home customer)
    public int? RoleId { get; private set; }
    public int? CompanyId { get; private set; }

    // credential
    public string? PasswordHash { get; private set; }
    public DateTime? PasswordChangedAt { get; private set; }
    public DateTime? PasswordExpiresAt { get; private set; }
    public bool ForcePasswordChange { get; private set; }

    // failed-login / lock state
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LastFailedLoginAt { get; private set; }
    public bool IsBlocked { get; private set; }
    public DateTime? LockedUntil { get; private set; }

    // login trail
    public DateTime? LastLoginAt { get; private set; }
    public string? LastLoginIp { get; private set; }

    // lifecycle
    public bool IsActive { get; private set; }

    // audit
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Password-history rows added in this unit of work by <see cref="SetPassword"/>; saved with the user.
    /// Existing rows are not loaded here — the repository reads the recent hashes for the reuse check.
    /// </summary>
    public IReadOnlyCollection<USER_PASSWORD_HISTORY> PasswordHistory => _passwordHistory;

    public static USERS Create(
        string userName,
        string passwordHash,
        int roleId,
        int? companyId,
        string? fullName,
        string? email,
        string? phone,
        string? employeeCode,
        bool forcePasswordChange,
        DateTime? passwordExpiresAt,
        DateTime now,
        int? createdBy)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("User name is required.", nameof(userName));
        }

        var user = new USERS
        {
            UserName = userName.Trim(),
            RoleId = roleId,
            CompanyId = companyId,
            FullName = Clean(fullName),
            Email = Clean(email),
            Phone = Clean(phone),
            EmployeeCode = Clean(employeeCode),
            IsActive = true,
            IsBlocked = false,
            FailedLoginAttempts = 0,
            CreatedAt = now,
            CreatedBy = createdBy,
        };

        user.SetPassword(passwordHash, passwordExpiresAt, forcePasswordChange, now, createdBy);
        return user;
    }

    public void UpdateProfile(
        string? fullName,
        string? email,
        string? phone,
        string? employeeCode,
        int roleId,
        int? companyId,
        DateTime now,
        int? updatedBy)
    {
        FullName = Clean(fullName);
        Email = Clean(email);
        Phone = Clean(phone);
        EmployeeCode = Clean(employeeCode);
        RoleId = roleId;
        CompanyId = companyId;
        Touch(now, updatedBy);
    }

    /// <summary>Stores a new hash and records the old state in history so reuse can be refused later.</summary>
    public void SetPassword(string passwordHash, DateTime? expiresAt, bool forceChange, DateTime now, int? changedBy)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        PasswordChangedAt = now;
        PasswordExpiresAt = expiresAt;
        ForcePasswordChange = forceChange;
        _passwordHistory.Add(USER_PASSWORD_HISTORY.Create(passwordHash, now));
        Touch(now, changedBy);
    }

    public bool IsLockedAt(DateTime now) => LockedUntil.HasValue && LockedUntil.Value > now;

    public bool IsPasswordExpiredAt(DateTime now) => PasswordExpiresAt.HasValue && PasswordExpiresAt.Value <= now;

    /// <summary>
    /// A wrong password. Attempts are counted per calendar day of <paramref name="now"/> (IST):
    /// a failure on a new day, or after an earlier lock has expired, starts again at 1.
    /// Returns true when this attempt locked the account.
    /// </summary>
    public bool RegisterFailedLogin(DateTime now, bool lockEnabled, int maxAttempts, int lockMinutes)
    {
        if (IsLockedAt(now))
        {
            return true;    // still locked: nothing to count, and the lock is never extended by more guesses
        }

        var lockExpired = LockedUntil.HasValue && LockedUntil.Value <= now;
        var sameDay = LastFailedLoginAt.HasValue
                      && DateOnly.FromDateTime(LastFailedLoginAt.Value) == DateOnly.FromDateTime(now);

        FailedLoginAttempts = sameDay && !lockExpired ? FailedLoginAttempts + 1 : 1;
        LastFailedLoginAt = now;

        if (lockExpired)
        {
            LockedUntil = null;
        }

        if (lockEnabled && FailedLoginAttempts >= maxAttempts)
        {
            LockedUntil = now.AddMinutes(lockMinutes);
            return true;
        }

        return false;
    }

    /// <summary>A correct password on an unlocked account: the failed-login slate is wiped.</summary>
    public void RegisterSuccessfulLogin(DateTime now, string? ipAddress)
    {
        FailedLoginAttempts = 0;
        LastFailedLoginAt = null;
        LockedUntil = null;
        LastLoginAt = now;
        LastLoginIp = ipAddress;
    }

    /// <summary>Administrator lifts a temporary lock AND a block.</summary>
    public void Unlock(DateTime now, int? updatedBy)
    {
        ClearTemporaryLock(now, updatedBy);
        IsBlocked = false;
    }

    /// <summary>Self-service (password recovery) may clear the failed-login lock, never an administrator's block.</summary>
    public void ClearTemporaryLock(DateTime now, int? updatedBy)
    {
        FailedLoginAttempts = 0;
        LastFailedLoginAt = null;
        LockedUntil = null;
        Touch(now, updatedBy);
    }

    /// <summary>May this account authenticate at all right now (active, not blocked, not temporarily locked)?</summary>
    public bool CanAuthenticateAt(DateTime now) => IsActive && !IsBlocked && !IsLockedAt(now);

    public void Activate(DateTime now, int? updatedBy)
    {
        IsActive = true;
        Touch(now, updatedBy);
    }

    public void Deactivate(DateTime now, int? updatedBy)
    {
        IsActive = false;
        Touch(now, updatedBy);
    }

    private void Touch(DateTime now, int? updatedBy)
    {
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
