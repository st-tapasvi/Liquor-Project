using FluentAssertions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Entities;

/// <summary>Failed-login counting, temporary lock and password state — the rules that must hold for every user.</summary>
public sealed class USERS_Tests
{
    private static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0);   // IST

    private const bool LockOn = true;
    private const int MaxAttempts = 3;
    private const int LockMinutes = 1440;

    private static USERS NewUser(bool forceChange = false, DateTime? expiresAt = null) =>
        USERS.Create(" alice ", "H:x", companyId: 2, "Alice", "a@x.com", "999", "E1",
            forceChange, expiresAt, Now, createdBy: 7);

    // ---------- creation ----------

    [Fact]
    public void Create_SetsDefaults_TrimsName_RecordsFirstHistoryRow()
    {
        var user = NewUser(forceChange: true, expiresAt: Now.AddDays(90));

        user.UserName.Should().Be("alice");
        user.IsActive.Should().BeTrue();
        user.IsBlocked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.ForcePasswordChange.Should().BeTrue();
        user.PasswordExpiresAt.Should().Be(Now.AddDays(90));
        user.PasswordChangedAt.Should().Be(Now);
        user.CreatedBy.Should().Be(7);
        user.PasswordHistory.Should().ContainSingle(h => h.PasswordHash == "H:x");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankUserName_Throws(string name)
    {
        var act = () => USERS.Create(name, "H:x", null, null, null, null, null, false, null, Now, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPassword_BlankHash_Throws()
    {
        var act = () => NewUser().SetPassword("", null, false, Now, null);
        act.Should().Throw<ArgumentException>();
    }

    // ---------- failed login: same day ----------

    [Fact]
    public void RegisterFailedLogin_FirstAttempt_CountsOne_DoesNotLock()
    {
        var user = NewUser();

        var locked = user.RegisterFailedLogin(Now, LockOn, MaxAttempts, LockMinutes);

        locked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(1);
        user.LastFailedLoginAt.Should().Be(Now);
        user.LockedUntil.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void RegisterFailedLogin_LocksOnExactlyTheNthAttempt_NotBefore(int maxAttempts)
    {
        var user = NewUser();

        for (var attempt = 1; attempt < maxAttempts; attempt++)
        {
            user.RegisterFailedLogin(Now.AddMinutes(attempt), LockOn, maxAttempts, LockMinutes).Should().BeFalse();
            user.LockedUntil.Should().BeNull();
        }

        user.RegisterFailedLogin(Now.AddMinutes(maxAttempts), LockOn, maxAttempts, LockMinutes).Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(maxAttempts);
    }

    [Fact]
    public void RegisterFailedLogin_WhileStillLocked_ChangesNothing_ReportsLocked()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, maxAttempts: 1, LockMinutes);
        var lockedUntil = user.LockedUntil;

        var result = user.RegisterFailedLogin(Now.AddHours(5), LockOn, maxAttempts: 1, LockMinutes);

        result.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(1);           // not incremented
        user.LockedUntil.Should().Be(lockedUntil);         // not extended (no sliding lock)
        user.LastFailedLoginAt.Should().Be(Now);           // not touched
    }

    [Fact]
    public void RegisterFailedLogin_ExactlyAtLockExpiry_CountsAsExpired_StartsAtOne()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, maxAttempts: 1, LockMinutes);
        var expiry = user.LockedUntil!.Value;

        var locked = user.RegisterFailedLogin(expiry, LockOn, maxAttempts: 3, LockMinutes);

        locked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(1);
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void RegisterFailedLogin_DayBoundaryToTheSecond_Resets()
    {
        var user = NewUser();
        user.RegisterFailedLogin(new DateTime(2026, 9, 27, 23, 59, 59), LockOn, MaxAttempts, LockMinutes);
        user.RegisterFailedLogin(new DateTime(2026, 9, 27, 23, 59, 59), LockOn, MaxAttempts, LockMinutes);

        user.RegisterFailedLogin(new DateTime(2026, 9, 28, 0, 0, 0), LockOn, MaxAttempts, LockMinutes).Should().BeFalse();

        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public void RegisterSuccessfulLogin_DoesNotLiftAnAdministratorBlock()
    {
        var user = NewUser();
        typeof(USERS).GetProperty(nameof(USERS.IsBlocked))!.SetValue(user, true);   // legacy IS_BLOCKED set by an admin

        user.RegisterSuccessfulLogin(Now, null);

        user.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public void RegisterFailedLogin_SameDay_Increments()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, MaxAttempts, LockMinutes);

        user.RegisterFailedLogin(Now.AddMinutes(5), LockOn, MaxAttempts, LockMinutes);

        user.FailedLoginAttempts.Should().Be(2);
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void RegisterFailedLogin_ThirdAttemptSameDay_LocksFor24Hours()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, MaxAttempts, LockMinutes);
        user.RegisterFailedLogin(Now.AddMinutes(5), LockOn, MaxAttempts, LockMinutes);

        var third = Now.AddMinutes(9);
        var locked = user.RegisterFailedLogin(third, LockOn, MaxAttempts, LockMinutes);

        locked.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(3);
        user.LockedUntil.Should().Be(third.AddMinutes(LockMinutes));
        user.IsLockedAt(third.AddHours(23)).Should().BeTrue();
    }

    // ---------- failed login: day boundary (IST) ----------

    [Fact]
    public void RegisterFailedLogin_NewDay_StartsAgainAtOne()
    {
        var user = NewUser();
        var yesterdayLate = new DateTime(2026, 9, 27, 23, 59, 0);
        user.RegisterFailedLogin(yesterdayLate, LockOn, MaxAttempts, LockMinutes);
        user.RegisterFailedLogin(yesterdayLate.AddSeconds(30), LockOn, MaxAttempts, LockMinutes);
        user.FailedLoginAttempts.Should().Be(2);

        var todayEarly = new DateTime(2026, 9, 28, 0, 1, 0);
        var locked = user.RegisterFailedLogin(todayEarly, LockOn, MaxAttempts, LockMinutes);

        locked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(1);      // yesterday's attempts do not carry over
    }

    [Fact]
    public void RegisterFailedLogin_LockDisabled_NeverLocks()
    {
        var user = NewUser();

        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedLogin(Now.AddMinutes(i), lockEnabled: false, MaxAttempts, LockMinutes).Should().BeFalse();
        }

        user.FailedLoginAttempts.Should().Be(5);
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void RegisterFailedLogin_AfterLockExpired_StartsAgainAtOne()
    {
        var user = NewUser();
        var shortLock = 30;
        user.RegisterFailedLogin(Now, LockOn, MaxAttempts, shortLock);
        user.RegisterFailedLogin(Now.AddMinutes(1), LockOn, MaxAttempts, shortLock);
        user.RegisterFailedLogin(Now.AddMinutes(2), LockOn, MaxAttempts, shortLock).Should().BeTrue();

        var afterLock = Now.AddMinutes(2 + shortLock + 1);        // lock has expired, still the same day
        var locked = user.RegisterFailedLogin(afterLock, LockOn, MaxAttempts, shortLock);

        locked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(1);
        user.LockedUntil.Should().BeNull();
    }

    // ---------- lock state ----------

    [Fact]
    public void IsLockedAt_TrueBeforeExpiry_FalseAtAndAfterExpiry()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, maxAttempts: 1, LockMinutes);
        var until = user.LockedUntil!.Value;

        user.IsLockedAt(until.AddSeconds(-1)).Should().BeTrue();
        user.IsLockedAt(until).Should().BeFalse();               // "locked until" is exclusive
        user.IsLockedAt(until.AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void RegisterSuccessfulLogin_WipesFailedLoginSlate_RecordsLogin()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, maxAttempts: 1, LockMinutes);
        user.LockedUntil.Should().NotBeNull();

        var later = Now.AddDays(2);
        user.RegisterSuccessfulLogin(later, "192.168.1.5");

        user.FailedLoginAttempts.Should().Be(0);
        user.LastFailedLoginAt.Should().BeNull();
        user.LockedUntil.Should().BeNull();
        user.LastLoginAt.Should().Be(later);
        user.LastLoginIp.Should().Be("192.168.1.5");
    }

    [Fact]
    public void ClearTemporaryLock_ClearsLockAndCounter_ButNeverAnAdministratorBlock()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, maxAttempts: 1, LockMinutes);
        typeof(USERS).GetProperty(nameof(USERS.IsBlocked))!.SetValue(user, true);

        user.ClearTemporaryLock(Now.AddMinutes(1), updatedBy: 10);

        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.IsBlocked.Should().BeTrue();                  // only Unlock (administrator) lifts a block
        user.CanAuthenticateAt(Now.AddMinutes(1)).Should().BeFalse();
    }

    [Fact]
    public void CanAuthenticateAt_RequiresActiveNotBlockedNotLocked()
    {
        var user = NewUser();
        user.CanAuthenticateAt(Now).Should().BeTrue();

        user.RegisterFailedLogin(Now, LockOn, maxAttempts: 1, lockMinutes: 30);
        user.CanAuthenticateAt(Now.AddMinutes(29)).Should().BeFalse();
        user.CanAuthenticateAt(Now.AddMinutes(30)).Should().BeTrue();

        user.Deactivate(Now, 1);
        user.CanAuthenticateAt(Now.AddMinutes(30)).Should().BeFalse();
    }

    [Fact]
    public void Unlock_ClearsLockCounterAndBlock()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now, LockOn, maxAttempts: 1, LockMinutes);

        user.Unlock(Now.AddMinutes(1), updatedBy: 99);

        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.IsBlocked.Should().BeFalse();
        user.UpdatedBy.Should().Be(99);
        user.UpdatedAt.Should().Be(Now.AddMinutes(1));
    }

    // ---------- password state ----------

    [Fact]
    public void IsPasswordExpiredAt_ExpiresAtIsInclusive()
    {
        var expires = Now.AddDays(90);
        var user = NewUser(expiresAt: expires);

        user.IsPasswordExpiredAt(expires.AddSeconds(-1)).Should().BeFalse();
        user.IsPasswordExpiredAt(expires).Should().BeTrue();
        NewUser(expiresAt: null).IsPasswordExpiredAt(Now.AddYears(10)).Should().BeFalse();
    }

    [Fact]
    public void SetPassword_ReplacesHash_AppendsHistory_ClearsForceFlag()
    {
        var user = NewUser(forceChange: true);

        user.SetPassword("H:y", Now.AddDays(90), forceChange: false, Now.AddHours(1), changedBy: 10);

        user.PasswordHash.Should().Be("H:y");
        user.ForcePasswordChange.Should().BeFalse();
        user.PasswordChangedAt.Should().Be(Now.AddHours(1));
        user.PasswordExpiresAt.Should().Be(Now.AddDays(90));
        user.PasswordHistory.Select(h => h.PasswordHash).Should().Equal("H:x", "H:y");
    }

    // ---------- lifecycle ----------

    [Fact]
    public void Deactivate_ThenActivate_TogglesAndStampsAudit()
    {
        var user = NewUser();

        user.Deactivate(Now, 5);
        user.IsActive.Should().BeFalse();
        user.UpdatedBy.Should().Be(5);

        user.Activate(Now.AddMinutes(1), 6);
        user.IsActive.Should().BeTrue();
        user.UpdatedBy.Should().Be(6);
        user.UpdatedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void UpdateProfile_TrimsAndBlanksToNull()
    {
        var user = NewUser();

        user.UpdateProfile(" Bob ", "  ", null, "E2", Now, 5);

        user.FullName.Should().Be("Bob");
        user.Email.Should().BeNull();
        user.EmployeeCode.Should().Be("E2");
        user.CompanyId.Should().Be(2);       // the home company is not part of a profile edit
    }
}
