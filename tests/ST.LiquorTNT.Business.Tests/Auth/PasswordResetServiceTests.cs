using FluentAssertions;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Auth;

public sealed class PasswordResetServiceTests
{
    private const string Answer = "New Delhi";           // stored normalised: FakePasswordHasher -> "H:newdelhi"

    private readonly FakeSecurityQuestionRepository _questions = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeSessionRepository _sessions = new();
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);
    private readonly FakeSecurityConfigProvider _config;
    private readonly PasswordResetService _service;
    private readonly USERS _alice;

    public PasswordResetServiceTests()
    {
        _config = new FakeSecurityConfigProvider(SecuritySettings.FromEntries(new Dictionary<string, string>
        {
            [SecuritySettings.Keys.SecurityQuestionEnabled] = "1",
            [SecuritySettings.Keys.PasswordResetExpiryMinutes] = "15",
            [SecuritySettings.Keys.PasswordResetMaxAttempts] = "3",
            [SecuritySettings.Keys.MaxFailedLoginAttempts] = "5",        // account lock needs more than the reset allows
            [SecuritySettings.Keys.AccountLockDurationMinutes] = "1440",
        }));

        _alice = TestData.User(id: 10, userName: "alice", passwordHash: "H:Secret@1");
        _users.Users.Add(_alice);
        _questions.Questions.Add(SeedRows.Question(1, "In which city were you born?"));
        _questions.UserQuestions.Add(USER_SECURITY_QUESTION.Create(10, 1, "H:" + SecurityAnswers.Normalize(Answer), TestData.Now).WithId(1));

        var hasher = new FakePasswordHasher();
        var rules = new PasswordRules(new FakePasswordPolicyRepository(TestData.Policy(minLength: 6, historyCount: 3)), _users, new PasswordPolicyValidator(hasher), _clock);

        _service = new PasswordResetService(_questions, _users, _sessions, new CredentialVerifier(_users, hasher, _log), hasher, new FakeTokenHasher(),
            _config, rules, _clock, _log,
            new ForgotPasswordStartRequestValidator(), new ForgotPasswordVerifyRequestValidator(), new ForgotPasswordResetRequestValidator());
    }

    private void Configure(string key, string value)
    {
        var entries = new Dictionary<string, string>
        {
            [SecuritySettings.Keys.SecurityQuestionEnabled] = "1",
            [SecuritySettings.Keys.PasswordResetExpiryMinutes] = "15",
            [SecuritySettings.Keys.PasswordResetMaxAttempts] = "3",
            [SecuritySettings.Keys.MaxFailedLoginAttempts] = "5",
            [key] = value,
        };
        _config.Settings = SecuritySettings.FromEntries(entries);
    }

    private Task<ForgotPasswordStartResponse> Start(string userName = "alice") =>
        _service.StartAsync(new ForgotPasswordStartRequest { UserName = userName }, CancellationToken.None);

    private Task Verify(string token, string answer) =>
        _service.VerifyAsync(new ForgotPasswordVerifyRequest { RequestToken = token, Answer = answer }, CancellationToken.None);

    private Task Reset(string token, string newPassword) =>
        _service.ResetAsync(new ForgotPasswordResetRequest { RequestToken = token, NewPassword = newPassword }, CancellationToken.None);

    private PASSWORD_RESET_REQUEST RequestFor(string token) => _questions.ResetRequests.Single(r => r.RequestTokenHash == "TH:" + token);

    // ---------- start ----------

    [Fact]
    public async Task Start_WhenDisabledInConfig_409()
    {
        Configure(SecuritySettings.Keys.SecurityQuestionEnabled, "0");

        var ex = await _service.Invoking(_ => Start()).Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.SecurityQuestionDisabled);
    }

    [Theory]
    [InlineData("nobody")]      // unknown user
    [InlineData("bob")]         // known user, no question set
    public async Task Start_UnknownUserOrNoQuestion_SameResponse_409(string userName)
    {
        _users.Users.Add(TestData.User(id: 11, userName: "bob"));

        var ex = await _service.Invoking(_ => Start(userName)).Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.SecurityQuestionNotSet);
        ex.Which.Message.Should().Be("No security question is set. Ask an administrator to reset the password.");   // byte-identical
        _questions.ResetRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_InactiveUser_403()
    {
        _alice.Deactivate(TestData.Now, 1);

        var ex = await _service.Invoking(_ => Start()).Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.UserInactive);
    }

    [Fact]
    public async Task Start_LockedUser_403_ALockIsALock()
    {
        _alice.RegisterFailedLogin(TestData.Now, true, 1, 1440);

        var ex = await _service.Invoking(_ => Start()).Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.UserLocked);
        _questions.ResetRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_Success_ReturnsQuestionAndToken_StoresOnlyHash_Audits()
    {
        var response = await Start();

        response.QuestionText.Should().Be("In which city were you born?");
        response.RequestToken.Should().NotBeNullOrWhiteSpace().And.NotContainAny("+", "/", "=");
        response.ExpiresAt.Should().Be(TestData.Now.AddMinutes(15));

        var stored = _questions.ResetRequests.Single();
        stored.RequestTokenHash.Should().Be("TH:" + response.RequestToken);
        stored.Status.Should().Be(PASSWORD_RESET_REQUEST.StatusPending);
        _log.Has(UserLogActions.PasswordResetRequested).Should().BeTrue();
        _log.Entries.Single().Description.Should().NotContain(response.RequestToken);    // token never audited
    }

    [Fact]
    public async Task Start_Again_SupersedesEarlierOpenRequest()
    {
        var first = await Start();

        var second = await Start();

        RequestFor(first.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusExpired);
        RequestFor(second.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusPending);
        await _service.Invoking(_ => Verify(first.RequestToken, Answer)).Should().ThrowAsync<BusinessException>();
    }

    // ---------- verify ----------

    [Fact]
    public async Task Verify_UnknownToken_409()
    {
        var ex = await _service.Invoking(_ => Verify("never-issued", Answer)).Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ResetRequestInvalid);
    }

    [Fact]
    public async Task Verify_Expired_409_MarksExpired()
    {
        var start = await Start();
        _clock.Advance(TimeSpan.FromMinutes(15));       // exactly at expiry -> expired

        var ex = await _service.Invoking(_ => Verify(start.RequestToken, Answer)).Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.ResetRequestExpired);
        RequestFor(start.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusExpired);
    }

    [Fact]
    public async Task Verify_AfterRecoveryDisabledMidFlow_409()
    {
        var start = await Start();
        Configure(SecuritySettings.Keys.SecurityQuestionEnabled, "0");

        var ex = await _service.Invoking(_ => Verify(start.RequestToken, Answer)).Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.SecurityQuestionDisabled);
    }

    [Fact]
    public async Task Verify_AfterUserDeactivatedMidFlow_403()
    {
        var start = await Start();
        _alice.Deactivate(TestData.Now, 1);

        var ex = await _service.Invoking(_ => Verify(start.RequestToken, Answer)).Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.UserInactive);
    }

    [Fact]
    public async Task Verify_WrongAnswer_401_CountsOnRequestAndOnAccount_Audits()
    {
        var start = await Start();

        var ex = await _service.Invoking(_ => Verify(start.RequestToken, "Mumbai")).Should().ThrowAsync<UnauthorizedException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.SecurityAnswerIncorrect);
        RequestFor(start.RequestToken).VerifyAttempts.Should().Be(1);
        RequestFor(start.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusPending);
        _alice.FailedLoginAttempts.Should().Be(1);                        // guessing answers is guessing credentials
        _log.Has(UserLogActions.PasswordResetFailed).Should().BeTrue();
        _log.Has(UserLogActions.LoginFailed).Should().BeTrue();
    }

    [Fact]
    public async Task Verify_ThirdWrongAnswer_403_RequestFails_EvenRightAnswerAfterwardsIsRefused()
    {
        var start = await Start();
        await _service.Invoking(_ => Verify(start.RequestToken, "wrong")).Should().ThrowAsync<UnauthorizedException>();
        await _service.Invoking(_ => Verify(start.RequestToken, "wrong")).Should().ThrowAsync<UnauthorizedException>();

        var ex = await _service.Invoking(_ => Verify(start.RequestToken, "wrong")).Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.ResetAttemptsExceeded);
        RequestFor(start.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusFailed);
        (await _service.Invoking(_ => Verify(start.RequestToken, Answer)).Should().ThrowAsync<BusinessException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ResetRequestInvalid);
    }

    [Fact]
    public async Task Verify_StartingOverDoesNotBuyFreshGuesses_AccountLocksAtN()
    {
        Configure(SecuritySettings.Keys.MaxFailedLoginAttempts, "3");
        var first = await Start();
        await _service.Invoking(_ => Verify(first.RequestToken, "wrong")).Should().ThrowAsync<UnauthorizedException>();
        await _service.Invoking(_ => Verify(first.RequestToken, "wrong")).Should().ThrowAsync<UnauthorizedException>();

        var second = await Start();                                                   // attacker restarts
        var ex = await _service.Invoking(_ => Verify(second.RequestToken, "wrong")).Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.UserLocked);                       // third wrong overall -> account locked
        _alice.IsLockedAt(_clock.IndiaNow).Should().BeTrue();
        (await _service.Invoking(_ => Start()).Should().ThrowAsync<ForbiddenException>()).Which.ErrorCode.Should().Be(ErrorCodes.UserLocked);
    }

    [Fact]
    public async Task Verify_MaxAttemptsOne_FirstWrongAnswerFailsTheRequest()
    {
        Configure(SecuritySettings.Keys.PasswordResetMaxAttempts, "1");
        var start = await Start();

        var ex = await _service.Invoking(_ => Verify(start.RequestToken, "wrong")).Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.ResetAttemptsExceeded);
    }

    [Theory]
    [InlineData("New Delhi")]
    [InlineData("new delhi")]
    [InlineData("  NEW   DELHI ")]
    [InlineData("newdelhi")]       // no space at all
    [InlineData("NewDelhi")]
    [InlineData("new\tdelhi")]
    [InlineData("new delhi")]     // non-breaking space
    public async Task Verify_CorrectAnswer_IgnoresCaseAndAnyWhitespace_MarksVerified(string answer)
    {
        var start = await Start();

        await Verify(start.RequestToken, answer);

        RequestFor(start.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusVerified);
        _alice.FailedLoginAttempts.Should().Be(0);
    }

    // ---------- reset ----------

    [Fact]
    public async Task Reset_WithoutVerify_409()
    {
        var start = await Start();

        var ex = await _service.Invoking(_ => Reset(start.RequestToken, "Fresh@9")).Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.ResetRequestInvalid);
        _alice.PasswordHash.Should().Be("H:Secret@1");
    }

    [Fact]
    public async Task Reset_ExactlyAtExpiry_409()
    {
        var start = await Start();
        await Verify(start.RequestToken, Answer);
        _clock.Advance(TimeSpan.FromMinutes(15));

        var ex = await _service.Invoking(_ => Reset(start.RequestToken, "Fresh@9")).Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.ResetRequestExpired);
    }

    [Fact]
    public async Task Reset_WeakOrReusedPassword_400_LeavesRequestVerified()
    {
        var start = await Start();
        await Verify(start.RequestToken, Answer);

        var weak = await _service.Invoking(_ => Reset(start.RequestToken, "abc")).Should().ThrowAsync<ValidationException>();
        weak.Which.Errors.Should().ContainKey("password");

        var reused = await _service.Invoking(_ => Reset(start.RequestToken, "Secret@1")).Should().ThrowAsync<ValidationException>();
        reused.Which.Errors["password"].Should().ContainSingle(m => m.Contains("last 3"));

        RequestFor(start.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusVerified);   // may still try again
    }

    [Fact]
    public async Task Reset_Success_SetsPassword_ClearsFailedCounter_RevokesSessions_ConsumesToken_Audits()
    {
        _alice.RegisterFailedLogin(TestData.Now, true, 5, 1440);          // two earlier wrong logins today (not yet locked)
        _alice.RegisterFailedLogin(TestData.Now, true, 5, 1440);
        var session = USER_SESSION.Create(10, "TH:old", TestData.Now, 60, TestData.Now.AddHours(1), null, null);
        await _sessions.AddAsync(session, CancellationToken.None);
        var start = await Start();
        await Verify(start.RequestToken, Answer);

        await Reset(start.RequestToken, "Fresh@9");

        _alice.PasswordHash.Should().Be("H:Fresh@9");
        _alice.ForcePasswordChange.Should().BeFalse();
        _alice.FailedLoginAttempts.Should().Be(0);
        _alice.LockedUntil.Should().BeNull();
        session.Status.Should().Be(USER_SESSION.StatusRevoked);
        RequestFor(start.RequestToken).Status.Should().Be(PASSWORD_RESET_REQUEST.StatusUsed);
        _log.Has(UserLogActions.PasswordResetSuccess).Should().BeTrue();

        // the token is single-use
        (await _service.Invoking(_ => Reset(start.RequestToken, "Other@7")).Should().ThrowAsync<BusinessException>())
            .Which.ErrorCode.Should().Be(ErrorCodes.ResetRequestInvalid);
    }
}
