using FluentAssertions;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Auth;

public sealed class SessionServiceTests
{
    private readonly FakeSessionRepository _sessions = new();
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);
    private readonly FakeRequestContext _request = new() { AccessToken = "tok-current" };
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        _service = new SessionService(_sessions, new FakeTokenHasher(), _clock, new FakeCurrentUser(userId: 10), _request, _log);
    }

    private async Task<USER_SESSION> AddSession(int userId, string tokenHash, int hoursValid = 8)
    {
        var session = USER_SESSION.Create(userId, tokenHash, TestData.Now, hoursValid * 60, TestData.Now.AddHours(hoursValid), "10.0.0.1", "ua");
        await _sessions.AddAsync(session, CancellationToken.None);
        return session;
    }

    [Fact]
    public async Task GetMySessions_ListsOnlyMyActiveOnes_FlagsCurrent()
    {
        await AddSession(10, "TH:tok-current");
        await AddSession(10, "TH:tok-other");
        (await AddSession(10, "TH:tok-ended")).Logout(TestData.Now);
        await AddSession(99, "TH:someone-else");

        var list = await _service.GetMySessionsAsync(CancellationToken.None);

        list.Should().HaveCount(2);
        list.Single(s => s.IsCurrent).Id.Should().Be(1);
        list.Should().NotContain(s => s.Id == 3 || s.Id == 4);
    }

    [Fact]
    public async Task RevokeMySession_OwnActiveSession_Revokes_Audits()
    {
        var mine = await AddSession(10, "TH:tok-other");

        await _service.RevokeMySessionAsync(mine.Id, CancellationToken.None);

        mine.Status.Should().Be(USER_SESSION.StatusRevoked);
        _log.Has(UserLogActions.SessionRevoked).Should().BeTrue();
    }

    [Fact]
    public async Task RevokeMySession_SomeoneElsesSession_404()
    {
        var theirs = await AddSession(99, "TH:theirs");

        await _service.Invoking(s => s.RevokeMySessionAsync(theirs.Id, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();

        theirs.Status.Should().Be(USER_SESSION.StatusActive);
    }

    [Fact]
    public async Task RevokeMySession_AlreadyEnded_NoOp()
    {
        var mine = await AddSession(10, "TH:tok-other");
        mine.Logout(TestData.Now);

        await _service.RevokeMySessionAsync(mine.Id, CancellationToken.None);

        mine.Status.Should().Be(USER_SESSION.StatusLoggedOut);
        _log.Entries.Should().BeEmpty();
    }
}
