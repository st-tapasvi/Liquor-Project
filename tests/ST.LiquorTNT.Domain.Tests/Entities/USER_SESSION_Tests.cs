using FluentAssertions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Entities;

public sealed class USER_SESSION_Tests
{
    private static readonly DateTime Now = new(2026, 9, 28, 9, 0, 0);
    private const int Idle = 60;

    /// <summary>Idle window 60 min, hard limit 24 h — the defaults.</summary>
    private static USER_SESSION NewSession(int idle = Idle, DateTime? limit = null) =>
        USER_SESSION.Create(7, "hash-1", Now, idle, limit ?? Now.AddHours(24), "10.0.0.1", "agent");

    [Fact]
    public void Create_IsActive_IdleDeadlineFirst_HardLimitKept()
    {
        var session = NewSession();

        session.Status.Should().Be(USER_SESSION.StatusActive);
        session.UserId.Should().Be(7);
        session.LoginAt.Should().Be(Now);
        session.LastActivityAt.Should().Be(Now);
        session.ExpiresAt.Should().Be(Now.AddMinutes(60));         // 09:00 + idle
        session.AbsoluteExpiresAt.Should().Be(Now.AddHours(24));   // hard limit
        session.LogoutAt.Should().BeNull();
        session.IsActiveAt(Now).Should().BeTrue();
    }

    [Fact]
    public void Create_IdleLongerThanLimit_DeadlineIsTheLimit()
    {
        var session = NewSession(idle: 120, limit: Now.AddMinutes(30));

        session.ExpiresAt.Should().Be(Now.AddMinutes(30));
    }

    [Fact]
    public void Create_BlankHash_Throws()
    {
        var act = () => USER_SESSION.Create(7, " ", Now, Idle, Now.AddHours(1), null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]      // limit == now
    [InlineData(-1)]     // limit in the past
    public void Create_LimitNotAfterNow_Throws(int minutes)
    {
        var act = () => USER_SESSION.Create(7, "h", Now, Idle, Now.AddMinutes(minutes), null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_IdleBelowOneMinute_Throws(int idle)
    {
        var act = () => USER_SESSION.Create(7, "h", Now, idle, Now.AddHours(1), null, null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------- sliding: the example the owner asked about (login 09:00, work until 16:00) ----------

    [Fact]
    public void WorkingAllDay_NeverTimesOut_WhileInsideTheLimit()
    {
        var session = NewSession();

        // a call every 30 minutes from 09:30 to 16:00 — never a gap longer than the idle window
        for (var at = Now.AddMinutes(30); at <= Now.Date.AddHours(16); at = at.AddMinutes(30))
        {
            session.IsActiveAt(at).Should().BeTrue($"working at {at:HH:mm}");
            session.Slide(at, Idle);
        }

        session.ExpiresAt.Should().Be(Now.Date.AddHours(17));              // 16:00 + 60 min
        session.LastActivityAt.Should().Be(Now.Date.AddHours(16));
    }

    [Fact]
    public void GapLongerThanTheIdleWindow_EndsTheSession_EvenMidDay()
    {
        var session = NewSession();
        session.Slide(Now.Date.AddHours(12).AddMinutes(15), Idle);        // last call 12:15

        session.IsActiveAt(Now.Date.AddHours(14)).Should().BeFalse();     // 14:00 is 105 minutes later
    }

    [Fact]
    public void Idle_DeadlineIsExclusive()
    {
        var session = NewSession();

        session.IsActiveAt(Now.AddMinutes(60).AddSeconds(-1)).Should().BeTrue();
        session.IsActiveAt(Now.AddMinutes(60)).Should().BeFalse();
        session.ReachedLimitAt(Now.AddMinutes(60)).Should().BeFalse();   // it timed out, it did not hit the limit
    }

    [Fact]
    public void Slide_NeverPassesTheHardLimit()
    {
        var session = NewSession(limit: Now.AddHours(24));
        var nearLimit = Now.AddHours(23).AddMinutes(30);

        session.Slide(nearLimit, Idle);

        session.ExpiresAt.Should().Be(Now.AddHours(24));                   // not 24:30
        session.IsActiveAt(Now.AddHours(24)).Should().BeFalse();
        session.ReachedLimitAt(Now.AddHours(24)).Should().BeTrue();
    }

    [Fact]
    public void Slide_UsesTheIdleWindowGiven_SoAnAdminChangeAppliesOnTheNextSlide()
    {
        var session = NewSession();

        session.Slide(Now.AddMinutes(10), idleMinutes: 30);

        session.ExpiresAt.Should().Be(Now.AddMinutes(40));
    }

    // ---------- ending a session ----------

    [Fact]
    public void Logout_EndsWithLoggedOutStatus()
    {
        var session = NewSession();

        session.Logout(Now.AddMinutes(5));

        session.Status.Should().Be(USER_SESSION.StatusLoggedOut);
        session.LogoutAt.Should().Be(Now.AddMinutes(5));
        session.IsActiveAt(Now.AddMinutes(6)).Should().BeFalse();
    }

    [Fact]
    public void Revoke_EndsWithRevokedStatus()
    {
        var session = NewSession();

        session.Revoke(Now.AddMinutes(5));

        session.Status.Should().Be(USER_SESSION.StatusRevoked);
        session.LogoutAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void Expire_MarksExpired_WithoutLogoutTime()
    {
        var session = NewSession();

        session.Expire();

        session.Status.Should().Be(USER_SESSION.StatusExpired);
        session.LogoutAt.Should().BeNull();
        session.IsActiveAt(Now).Should().BeFalse();
    }
}
