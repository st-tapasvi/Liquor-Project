using FluentAssertions;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Auth;

public sealed class SecurityQuestionServiceTests
{
    private readonly FakeSecurityQuestionRepository _questions = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeUserLogWriter _log = new();
    private readonly FakeSessionRepository _sessions = new();
    private readonly USERS _alice;
    private readonly SecurityQuestionService _service;

    public SecurityQuestionServiceTests()
    {
        _alice = TestData.User(id: 10, userName: "alice", passwordHash: "H:Secret@1");
        _users.Users.Add(_alice);
        _questions.Questions.Add(SeedRows.Question(1, "First school?"));
        _questions.Questions.Add(SeedRows.Question(2, "First pet?"));
        _questions.Questions.Add(SeedRows.Question(3, "Retired question", active: false));

        _service = Build(new FakeCurrentUser(userId: 10, userName: "alice"));
    }

    private SecurityQuestionService Build(FakeCurrentUser caller)
    {
        var hasher = new FakePasswordHasher();
        return new SecurityQuestionService(_questions, _sessions, new CredentialVerifier(_users, hasher, _log), new FakeSecurityConfigProvider(),
            hasher, new FixedClock(TestData.Now), caller, _log, new SetSecurityQuestionRequestValidator());
    }

    private Task SetMine(int questionId = 1, string answer = "St. Mary's", string password = "Secret@1") =>
        _service.SetMyQuestionAsync(new SetSecurityQuestionRequest { QuestionId = questionId, Answer = answer, CurrentPassword = password }, CancellationToken.None);

    [Fact]
    public async Task GetQuestions_ReturnsActiveOnly_WithoutAnswers()
    {
        var list = await _service.GetQuestionsAsync(CancellationToken.None);

        list.Select(q => q.Id).Should().Equal(1, 2);
        typeof(SecurityQuestionResponse).GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Answer"));
    }

    [Fact]
    public async Task SetMine_WrongCurrentPassword_401_CountsAsFailedLogin_NothingStored()
    {
        var ex = await _service.Invoking(_ => SetMine(password: "wrong")).Should().ThrowAsync<UnauthorizedException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.InvalidCredentials);
        _alice.FailedLoginAttempts.Should().Be(1);          // a hijacked session cannot guess the password for free
        _questions.UserQuestions.Should().BeEmpty();
    }

    [Fact]
    public async Task SetMine_WhileLocked_403_EvenWithCorrectPassword()
    {
        _alice.RegisterFailedLogin(TestData.Now, true, 1, 1440);

        var ex = await _service.Invoking(_ => SetMine()).Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.UserLocked);
    }

    [Theory]
    [InlineData(3)]     // inactive question
    [InlineData(99)]    // unknown question
    public async Task SetMine_UnavailableQuestion_404(int questionId)
    {
        await _service.Invoking(_ => SetMine(questionId)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SetMine_Success_StoresNormalisedHash_RetiresPreviousChoice_Audits()
    {
        await SetMine(questionId: 1, answer: "  St. Mary's ");
        await SetMine(questionId: 2, answer: "Rex");

        var rows = _questions.UserQuestions.Where(q => q.UserId == 10).ToList();
        rows.Should().HaveCount(2);
        rows.Single(q => q.QuestionId == 1).IsActive.Should().BeFalse();
        var active = rows.Single(q => q.IsActive);
        active.QuestionId.Should().Be(2);
        active.AnswerHash.Should().Be("H:rex");                       // normalised (lower-case, no spaces), hashed
        _log.Entries.Should().HaveCount(2).And.OnlyContain(e => e.ActionType == UserLogActions.SecurityQuestionChanged);
        _log.Entries.Should().OnlyContain(e => !e.Description!.Contains("Rex", StringComparison.OrdinalIgnoreCase));   // answer never audited
    }

    [Fact]
    public async Task SetMine_SameQuestionAgainOrFormerQuestion_UpdatesInPlace_NoDuplicateRow()
    {
        await SetMine(questionId: 1, answer: "first");
        await SetMine(questionId: 1, answer: "changed");                 // change the answer
        await SetMine(questionId: 2, answer: "pet");
        await SetMine(questionId: 1, answer: "back again");              // return to a former question

        var rows = _questions.UserQuestions.Where(q => q.UserId == 10).ToList();
        rows.Should().HaveCount(2);                                      // one row per (user, question), never a duplicate
        rows.Single(q => q.QuestionId == 1).Should().Match<USER_SECURITY_QUESTION>(q => q.IsActive && q.AnswerHash == "H:backagain");
        rows.Single(q => q.QuestionId == 2).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SetMine_FirstLogin_ReleasesSessionsHeldOnQuestionScreen()
    {
        var session = USER_SESSION.Create(10, "TH:tok", TestData.Now.AddMinutes(-5), 60, TestData.Now.AddHours(8), null, null);
        session.RequireSecurityQuestion();
        _sessions.Sessions.Add(session);

        await SetMine();

        session.SecurityQuestionPending.Should().BeFalse();
        _sessions.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task SetMine_NotAuthenticated_401()
    {
        var anonymous = Build(new FakeCurrentUser(userId: null, userName: null));

        await anonymous.Invoking(s => s.SetMyQuestionAsync(new SetSecurityQuestionRequest { QuestionId = 1, Answer = "xy", CurrentPassword = "y" }, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }
}
