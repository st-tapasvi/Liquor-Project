using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using ST.LiquorTNT.Api.Middleware;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>
/// Which way a web session ends decides what the screen does: idle → login page (SESSION_TIMED_OUT),
/// hard limit → password popup + retry (SESSION_EXPIRED), logout/revoke → login page (SESSION_INVALID).
/// </summary>
public sealed class SessionValidationMiddlewareTests
{
    private static readonly DateTime LoginAt = new(2026, 9, 28, 9, 0, 0);

    private readonly FakeSessions _sessions = new();
    private readonly FakeConfig _config = new();
    private readonly FakeLog _log = new();
    private readonly FakeClock _clock = new();

    private USER_SESSION AddSession(int idle = 60, int limitHours = 24)
    {
        var session = USER_SESSION.Create(7, "TH:tok", LoginAt, idle, LoginAt.AddHours(limitHours), null, null);
        _sessions.Session = session;
        return session;
    }

    private async Task<bool> Call(DateTime at)
    {
        _clock.IndiaNow = at;
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "7") }, "test")),
        };
        var reachedController = false;

        var middleware = new SessionValidationMiddleware(_ => { reachedController = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, _sessions, new FakeHasher(), new FakeRequest(), _config, _clock, _log);
        return reachedController;
    }

    private async Task<string> Refused(DateTime at) =>
        (await this.Invoking(t => t.Call(at)).Should().ThrowAsync<UnauthorizedException>()).Which.ErrorCode;

    [Fact]
    public async Task WorkingFrom9To16_NeverRefused_DeadlineSlides()
    {
        var session = AddSession();

        // a call every 30 minutes from 09:30 to 16:00 — never a gap longer than the idle window
        for (var at = LoginAt.AddMinutes(30); at <= LoginAt.Date.AddHours(16); at = at.AddMinutes(30))
        {
            (await Call(at)).Should().BeTrue($"working at {at:HH:mm}");
        }

        session.ExpiresAt.Should().Be(LoginAt.Date.AddHours(17));
        session.Status.Should().Be(USER_SESSION.StatusActive);
    }

    [Fact]
    public async Task IdleLongerThanTheWindow_SessionTimedOut_GoesToLoginPage()
    {
        var session = AddSession();

        (await Refused(LoginAt.AddMinutes(61))).Should().Be(ErrorCodes.SessionTimedOut);

        session.Status.Should().Be(USER_SESSION.StatusExpired);
        _log.Entries.Should().ContainSingle(e => e.ActionType == UserLogActions.SessionExpired && e.Description!.Contains("idle"));
    }

    [Fact]
    public async Task HardLimitReachedWhileWorking_SessionExpired_PopupAndRetry()
    {
        var session = AddSession(limitHours: 24);

        // working every 30 minutes for 24 hours: never idle, but the hard limit still ends it
        for (var at = LoginAt.AddMinutes(30); at < LoginAt.AddHours(24); at = at.AddMinutes(30))
        {
            (await Call(at)).Should().BeTrue();
        }

        (await Refused(LoginAt.AddHours(24))).Should().Be(ErrorCodes.SessionExpired);
        session.Status.Should().Be(USER_SESSION.StatusExpired);
        _log.Entries.Should().Contain(e => e.ActionType == UserLogActions.SessionExpired && e.Description!.Contains("hard limit"));
    }

    [Theory]
    [InlineData("logout")]
    [InlineData("revoke")]
    [InlineData("unknown")]
    public async Task EndedOrUnknownSession_SessionInvalid(string how)
    {
        var session = AddSession();
        switch (how)
        {
            case "logout": session.Logout(LoginAt); break;
            case "revoke": session.Revoke(LoginAt); break;
            default: _sessions.Session = null; break;
        }

        (await Refused(LoginAt.AddMinutes(5))).Should().Be(ErrorCodes.SessionInvalid);
    }

    [Fact]
    public async Task ActivityWrittenAtMostOncePerMinute_ConfigReadOnlyThen()
    {
        AddSession();

        await Call(LoginAt.AddSeconds(20));      // within the first minute: nothing to write
        await Call(LoginAt.AddSeconds(40));
        _sessions.Saves.Should().Be(0);
        _config.Reads.Should().Be(0);

        await Call(LoginAt.AddMinutes(1));       // a minute later: slide once
        _sessions.Saves.Should().Be(1);
        _config.Reads.Should().Be(1);
    }

    [Fact]
    public async Task AdminChangesIdleWindow_AppliesOnTheNextSlide()
    {
        var session = AddSession();
        _config.IdleMinutes = 15;

        await Call(LoginAt.AddMinutes(5));

        session.ExpiresAt.Should().Be(LoginAt.AddMinutes(20));
    }

    // ---------- small fakes ----------

    private sealed class FakeSessions : ISessionRepository
    {
        public USER_SESSION? Session { get; set; }
        public int Saves { get; private set; }

        public Task<USER_SESSION?> GetByTokenHashAsync(string tokenHash, CancellationToken ct) =>
            Task.FromResult(Session?.SessionTokenHash == tokenHash ? Session : null);

        public Task SaveChangesAsync(CancellationToken ct)
        {
            Saves++;
            return Task.CompletedTask;
        }

        public Task AddAsync(USER_SESSION session, CancellationToken ct) => throw new NotSupportedException();
        public Task<USER_SESSION?> GetByIdForUserAsync(int sessionId, int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<USER_SESSION>> GetActiveForUserAsync(int userId, DateTime now, CancellationToken ct) => throw new NotSupportedException();
        public Task<int> CountActiveAsync(int userId, DateTime now, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeConfig : ISecurityConfigProvider
    {
        public int IdleMinutes { get; set; } = 60;
        public int Reads { get; private set; }

        public Task<SecuritySettings> GetAsync(CancellationToken ct)
        {
            Reads++;
            return Task.FromResult(SecuritySettings.FromEntries(new Dictionary<string, string>
            {
                [SecuritySettings.Keys.SessionIdleMinutes] = IdleMinutes.ToString(),
            }));
        }
    }

    private sealed class FakeLog : IUserLogWriter
    {
        public List<UserLogEntry> Entries { get; } = new();

        public Task WriteAsync(UserLogEntry entry, CancellationToken ct)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTime IndiaNow { get; set; }
        public DateTime UtcNow => IndiaNow.AddMinutes(-330);
    }

    private sealed class FakeHasher : ITokenHasher
    {
        public string Hash(string token) => "TH:" + token;
    }

    private sealed class FakeRequest : IRequestContext
    {
        public string? IpAddress => null;
        public string? UserAgent => null;
        public string? CorrelationId => null;
        public string? AccessToken => "tok";
    }
}
