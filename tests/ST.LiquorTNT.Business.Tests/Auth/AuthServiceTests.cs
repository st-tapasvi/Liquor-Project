using FluentAssertions;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Auth;

public sealed class AuthServiceTests
{
    private const string Password = "Secret@1";                 // FakePasswordHasher: hash == "H:Secret@1"

    private readonly FakeUserRepository _users = new();
    private readonly FakeSessionRepository _sessions = new();
    private readonly FakeAccessTokenService _tokens = new();
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);
    private readonly FakeRequestContext _request = new();
    private readonly FakeSecurityConfigProvider _config;
    private readonly FakeCurrentUser _currentUser = new(userId: 10, userName: "alice");
    private readonly FakeAccessRepository _access = new();
    private readonly FakeSecurityQuestionRepository _questions = new();     // empty master list → no question step
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _config = new FakeSecurityConfigProvider(SecuritySettings.FromEntries(new Dictionary<string, string>
        {
            [SecuritySettings.Keys.FailedLoginLockEnabled] = "1",
            [SecuritySettings.Keys.MaxFailedLoginAttempts] = "3",
            [SecuritySettings.Keys.AccountLockDurationMinutes] = "1440",
            [SecuritySettings.Keys.SessionLimitEnabled] = "1",
            [SecuritySettings.Keys.MaxActiveSessions] = "2",
            [SecuritySettings.Keys.SessionExpiryMinutes] = "480",
            [SecuritySettings.Keys.SessionIdleMinutes] = "60",
        }));

        var hasher = new FakePasswordHasher();
        var policies = new FakePasswordPolicyRepository(TestData.Policy(minLength: 6, historyCount: 3));
        var rules = new PasswordRules(policies, _users, new PasswordPolicyValidator(hasher), _clock);

        _service = new AuthService(_users, _sessions, new CredentialVerifier(_users, hasher, _log), hasher, _tokens, new FakeTokenHasher(),
            _config, rules, _clock, _currentUser, _request, _log, new SupplierCodeDirectory(_access), _questions,
            new LoginRequestValidator(), new ChangePasswordRequestValidator());
    }

    private USERS AddUser(bool forceChange = false, DateTime? expiresAt = null)
    {
        var user = TestData.User(id: 10, userName: "alice", passwordHash: "H:" + Password, forceChange: forceChange, expiresAt: expiresAt);
        _users.Users.Add(user);
        return user;
    }

    private Task<LoginResponse> Login(string password = Password, string userName = "alice") =>
        _service.LoginAsync(new LoginRequest { UserName = userName, Password = password }, CancellationToken.None);

    private async Task<TException> LoginShouldFail<TException>(string expectedCode, string password = Password, string userName = "alice")
        where TException : AppException
    {
        var ex = await _service.Invoking(_ => Login(password, userName)).Should().ThrowAsync<TException>();
        ex.Which.ErrorCode.Should().Be(expectedCode);
        return ex.Which;
    }

    // ---------- who may not log in ----------

    [Fact]
    public async Task Login_UnknownUser_401_SameCodeAsWrongPassword_AuditedWithoutUserId()
    {
        await LoginShouldFail<UnauthorizedException>(ErrorCodes.InvalidCredentials, userName: "nobody");

        var entry = _log.Entries.Single();
        entry.ActionType.Should().Be(UserLogActions.LoginFailed);
        entry.ActorUserId.Should().BeNull();
        entry.Description.Should().NotContain("nobody");     // do not echo the attempted name
    }

    [Fact]
    public async Task Login_InactiveUser_403_EvenWithCorrectPassword()
    {
        AddUser().Deactivate(TestData.Now, 1);

        await LoginShouldFail<ForbiddenException>(ErrorCodes.UserInactive);
        _sessions.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_BlockedUser_403()
    {
        AddUser().With(nameof(USERS.IsBlocked), true);

        await LoginShouldFail<ForbiddenException>(ErrorCodes.UserBlocked);
    }

    // ---------- wrong password, counting and locking ----------

    [Fact]
    public async Task Login_WrongPassword_401_CountsAttempt_Audits()
    {
        var user = AddUser();

        await LoginShouldFail<UnauthorizedException>(ErrorCodes.InvalidCredentials, password: "wrong");

        user.FailedLoginAttempts.Should().Be(1);
        user.LockedUntil.Should().BeNull();
        _users.SaveCount.Should().Be(1);
        _log.Has(UserLogActions.LoginFailed).Should().BeTrue();
        _log.Has(UserLogActions.AccountLocked).Should().BeFalse();
    }

    [Fact]
    public async Task Login_ThirdWrongPasswordSameDay_LocksFor24h_403()
    {
        var user = AddUser();
        await LoginShouldFail<UnauthorizedException>(ErrorCodes.InvalidCredentials, password: "wrong");
        _clock.Advance(TimeSpan.FromMinutes(5));
        await LoginShouldFail<UnauthorizedException>(ErrorCodes.InvalidCredentials, password: "wrong");
        _clock.Advance(TimeSpan.FromMinutes(5));

        var ex = await LoginShouldFail<ForbiddenException>(ErrorCodes.UserLocked, password: "wrong");

        user.FailedLoginAttempts.Should().Be(3);
        user.LockedUntil.Should().Be(_clock.IndiaNow.AddMinutes(1440));
        ex.Title.Should().Be("Account locked after 3 failed attempts.");
        ex.Detail.Should().Contain(user.LockedUntil!.Value.ToString("yyyy-MM-dd HH:mm"));
        _log.Has(UserLogActions.AccountLocked).Should().BeTrue();
    }

    [Fact]
    public async Task Login_FourthWrongPassword_SaysAlreadyLocked_SameUntil_NotCounted()
    {
        var user = AddUser();
        for (var i = 0; i < 3; i++)
        {
            await _service.Invoking(_ => Login("wrong")).Should().ThrowAsync<AppException>();
        }
        var lockedUntil = user.LockedUntil;
        _clock.Advance(TimeSpan.FromMinutes(10));

        var ex = await LoginShouldFail<ForbiddenException>(ErrorCodes.UserLocked, password: "wrong");

        ex.Title.Should().Be("This account is already locked.");
        ex.Detail.Should().Contain(lockedUntil!.Value.ToString("yyyy-MM-dd HH:mm"));
        user.LockedUntil.Should().Be(lockedUntil);          // more guesses never extend the lock
        user.FailedLoginAttempts.Should().Be(3);
    }

    [Fact]
    public async Task Login_WhileLocked_CorrectPasswordIsStillRejected_403()
    {
        var user = AddUser();
        for (var i = 0; i < 3; i++)
        {
            await _service.Invoking(_ => Login("wrong")).Should().ThrowAsync<AppException>();
        }
        user.IsLockedAt(_clock.IndiaNow).Should().BeTrue();

        _clock.Advance(TimeSpan.FromHours(23));                          // still inside the 24h lock
        await LoginShouldFail<ForbiddenException>(ErrorCodes.UserLocked);   // correct password

        _sessions.Sessions.Should().BeEmpty();
        user.FailedLoginAttempts.Should().Be(3);                          // a locked attempt is not counted again
    }

    [Fact]
    public async Task Login_AfterLockExpires_CorrectPasswordSucceeds_AndResets()
    {
        var user = AddUser();
        for (var i = 0; i < 3; i++)
        {
            await _service.Invoking(_ => Login("wrong")).Should().ThrowAsync<AppException>();
        }

        _clock.Advance(TimeSpan.FromMinutes(1441));

        var response = await Login();

        response.AccessToken.Should().NotBeNullOrEmpty();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task Login_LockDisabledInConfig_ManyWrongPasswords_NeverLocks()
    {
        _config.Settings = SecuritySettings.FromEntries(new Dictionary<string, string>
        {
            [SecuritySettings.Keys.FailedLoginLockEnabled] = "0",
        });
        var user = AddUser();

        for (var i = 0; i < 6; i++)
        {
            await LoginShouldFail<UnauthorizedException>(ErrorCodes.InvalidCredentials, password: "wrong");
        }

        user.FailedLoginAttempts.Should().Be(6);
        user.LockedUntil.Should().BeNull();
    }

    // ---------- password state ----------

    [Fact]
    public async Task Login_ForcePasswordChange_403_NoSessionCreated()
    {
        AddUser(forceChange: true);

        await LoginShouldFail<ForbiddenException>(ErrorCodes.PasswordChangeRequired);
        _sessions.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Login_ExpiredPassword_403()
    {
        AddUser(expiresAt: TestData.Now.AddDays(-1));

        await LoginShouldFail<ForbiddenException>(ErrorCodes.PasswordExpired);
    }

    // ---------- session limit ----------

    [Fact]
    public async Task Login_SessionLimitReached_409_NoNewSession_Audited()
    {
        AddUser();
        await Login();
        await Login();
        _sessions.Sessions.Count(s => s.IsActiveAt(_clock.IndiaNow)).Should().Be(2);

        var ex = await _service.Invoking(_ => Login()).Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.SessionLimitReached);
        _sessions.Sessions.Should().HaveCount(2);
        _log.Entries.Should().Contain(e => e.ActionType == UserLogActions.LoginFailed && e.Description!.Contains("sessions"));
    }

    [Fact]
    public async Task Login_SessionLimitDisabled_AllowsMoreSessions()
    {
        _config.Settings = SecuritySettings.FromEntries(new Dictionary<string, string>
        {
            [SecuritySettings.Keys.SessionLimitEnabled] = "0",
            [SecuritySettings.Keys.MaxActiveSessions] = "1",
        });
        AddUser();

        await Login();
        await Login();
        await Login();

        _sessions.Sessions.Should().HaveCount(3);
    }

    [Fact]
    public async Task Login_EndedSessionsDoNotCountTowardsLimit()
    {
        AddUser();
        await Login();
        var second = await Login();
        var hash = "TH:" + second.AccessToken;
        _sessions.Sessions.Single(s => s.SessionTokenHash == hash).Logout(_clock.IndiaNow);

        await _service.Invoking(_ => Login()).Should().NotThrowAsync();
    }

    // ---------- success ----------

    [Fact]
    public async Task Login_Success_IssuesToken_StoresOnlyHash_ResetsCounter_Audits()
    {
        var user = AddUser();
        user.RegisterFailedLogin(TestData.Now, true, 3, 1440);          // one earlier wrong attempt today

        var response = await Login();

        response.AccessToken.Should().Be("tok-alice-1");
        response.ExpiresAt.Should().Be(TestData.Now.AddMinutes(480));            // the hard limit, for the client's warning
        response.IdleTimeoutMinutes.Should().Be(60);
        response.User.UserId.Should().Be(10);
        response.User.UserName.Should().Be("alice");

        var session = _sessions.Sessions.Single();
        session.SessionTokenHash.Should().Be("TH:tok-alice-1");         // never the raw token
        session.ExpiresAt.Should().Be(TestData.Now.AddMinutes(60));              // sliding: idle window first
        session.AbsoluteExpiresAt.Should().Be(TestData.Now.AddMinutes(480));     // hard limit = JWT expiry
        session.IpAddress.Should().Be("10.0.0.1");
        _tokens.LastExpiresAtUtc.Should().Be(_clock.UtcNow.AddMinutes(480));

        user.FailedLoginAttempts.Should().Be(0);
        user.LastLoginAt.Should().Be(TestData.Now);
        user.LastLoginIp.Should().Be("10.0.0.1");

        _log.Has(UserLogActions.LoginSuccess).Should().BeTrue();
        _log.Has(UserLogActions.SessionCreated).Should().BeTrue();
        _log.Entries.Should().OnlyContain(e => e.OldValue == null && e.NewValue == null);   // nothing sensitive serialised
    }

    // ---------- supplier code screen after every login ----------

    [Fact]
    public async Task Login_EvenWithOneSupplierCode_NothingIsPicked_UserGoesToTheSupplierCodeScreen()
    {
        AddUser();
        _access.SupplierCodesByUser[10] = new() { FakeAccessRepository.SupplierCode(30) };

        var response = await Login();

        response.SupplierCodes.Select(s => s.Id).Should().Equal(30);
        response.ActiveSupplierCode.Should().BeNull();
        _sessions.Sessions.Single().ActiveSupplierCodeId.Should().BeNull();
    }

    // ---------- security question on first login ----------

    [Fact]
    public async Task Login_NoSecurityQuestionYet_SessionHeldOnQuestionScreen()
    {
        AddUser();
        _questions.Questions.Add(SeedRows.Question(1, "First school?"));

        var response = await Login();

        response.User.SecurityQuestionRequired.Should().BeTrue();
        _sessions.Sessions.Single().SecurityQuestionPending.Should().BeTrue();
    }

    [Fact]
    public async Task Login_SecurityQuestionAlreadySet_NoQuestionStep()
    {
        AddUser();
        _questions.Questions.Add(SeedRows.Question(1, "First school?"));
        _questions.UserQuestions.Add(USER_SECURITY_QUESTION.Create(10, 1, "H:x", TestData.Now));

        var response = await Login();

        response.User.SecurityQuestionRequired.Should().BeFalse();
        _sessions.Sessions.Single().SecurityQuestionPending.Should().BeFalse();
    }

    [Fact]
    public async Task Login_SecurityQuestionsSwitchedOff_NoQuestionStep()
    {
        AddUser();
        _questions.Questions.Add(SeedRows.Question(1, "First school?"));
        _config.Settings = SecuritySettings.FromEntries(new Dictionary<string, string> { [SecuritySettings.Keys.SecurityQuestionEnabled] = "0" });

        var response = await Login();

        response.User.SecurityQuestionRequired.Should().BeFalse();
        _sessions.Sessions.Single().SecurityQuestionPending.Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentUser_NoSecurityQuestionYet_SaysRequired()
    {
        AddUser();
        _questions.Questions.Add(SeedRows.Question(1, "First school?"));

        (await _service.GetCurrentUserAsync(CancellationToken.None)).SecurityQuestionRequired.Should().BeTrue();
    }

    // ---------- logout ----------

    [Fact]
    public async Task Logout_EndsCurrentSession_Audits()
    {
        AddUser();
        var login = await Login();
        _request.AccessToken = login.AccessToken;

        await _service.LogoutAsync(CancellationToken.None);

        _sessions.Sessions.Single().Status.Should().Be(USER_SESSION.StatusLoggedOut);
        _log.Has(UserLogActions.Logout).Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WithoutToken_401()
    {
        _request.AccessToken = null;

        await _service.Invoking(s => s.LogoutAsync(CancellationToken.None)).Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Logout_UnknownOrEndedSession_IsIdempotent()
    {
        _request.AccessToken = "never-issued";

        await _service.Invoking(s => s.LogoutAsync(CancellationToken.None)).Should().NotThrowAsync();
        _log.Entries.Should().BeEmpty();
    }

    // ---------- current user ----------

    [Fact]
    public async Task GetCurrentUser_ReturnsCallerWithoutSecrets()
    {
        AddUser();

        var me = await _service.GetCurrentUserAsync(CancellationToken.None);

        me.UserId.Should().Be(10);
        me.UserName.Should().Be("alice");
        System.Text.Json.JsonSerializer.Serialize(me).Should().NotContainAny("Hash", "H:Secret");
    }

    [Fact]
    public async Task GetCurrentUser_NotAuthenticated_401()
    {
        var anonymous = new AuthService(_users, _sessions, new CredentialVerifier(_users, new FakePasswordHasher(), _log), new FakePasswordHasher(),
            _tokens, new FakeTokenHasher(), _config,
            new PasswordRules(new FakePasswordPolicyRepository(), _users, new PasswordPolicyValidator(new FakePasswordHasher()), _clock),
            _clock, new FakeCurrentUser(userId: null, userName: null), _request, _log, new SupplierCodeDirectory(_access), _questions,
            new LoginRequestValidator(), new ChangePasswordRequestValidator());

        await anonymous.Invoking(s => s.GetCurrentUserAsync(CancellationToken.None)).Should().ThrowAsync<UnauthorizedException>();
    }

    // ---------- change password ----------

    private Task ChangePassword(string current, string next) =>
        _service.ChangePasswordAsync(new ChangePasswordRequest { UserName = "alice", CurrentPassword = current, NewPassword = next }, CancellationToken.None);

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_401_CountsAsFailedAttempt()
    {
        var user = AddUser();

        var ex = await _service.Invoking(_ => ChangePassword("wrong", "Another@2")).Should().ThrowAsync<UnauthorizedException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidCredentials);
        user.FailedLoginAttempts.Should().Be(1);        // this endpoint cannot be used to brute-force
        user.PasswordHash.Should().Be("H:" + Password);
    }

    [Fact]
    public async Task ChangePassword_WhileLocked_403()
    {
        var user = AddUser();
        user.RegisterFailedLogin(TestData.Now, true, 1, 1440);

        var ex = await _service.Invoking(_ => ChangePassword(Password, "Another@2")).Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.UserLocked);
    }

    [Fact]
    public async Task ChangePassword_SameAsCurrent_400()
    {
        AddUser();

        var ex = await _service.Invoking(_ => ChangePassword(Password, Password)).Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("newPassword");
    }

    [Fact]
    public async Task ChangePassword_ReusingRecentPassword_400OnPasswordField()
    {
        var user = AddUser();
        user.SetPassword("H:Older@1", null, false, TestData.Now.AddDays(-10), 10);   // history: Secret@1, Older@1
        user.SetPassword("H:" + Password, null, false, TestData.Now.AddDays(-5), 10);

        var ex = await _service.Invoking(_ => ChangePassword(Password, "Older@1")).Should().ThrowAsync<ValidationException>();

        ex.Which.Errors["password"].Should().ContainSingle(m => m.Contains("last 3"));
    }

    [Fact]
    public async Task ChangePassword_Success_StoresNewHash_ClearsForceFlag_RevokesSessions_Audits()
    {
        var user = AddUser(forceChange: true);
        var session = USER_SESSION.Create(10, "TH:old", TestData.Now, 60, TestData.Now.AddHours(1), null, null);
        await _sessions.AddAsync(session, CancellationToken.None);

        await ChangePassword(Password, "Fresh@9");

        user.PasswordHash.Should().Be("H:Fresh@9");
        user.ForcePasswordChange.Should().BeFalse();
        user.PasswordChangedAt.Should().Be(TestData.Now);
        user.PasswordHistory.Select(h => h.PasswordHash).Should().EndWith("H:Fresh@9");
        session.Status.Should().Be(USER_SESSION.StatusRevoked);
        _log.Has(UserLogActions.PasswordChanged).Should().BeTrue();

        // and the new password now logs in
        var response = await Login("Fresh@9");
        response.AccessToken.Should().NotBeNullOrEmpty();
    }
}
