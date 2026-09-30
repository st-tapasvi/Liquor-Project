using System.Text.Json;
using FluentAssertions;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Users;

public sealed class UserServiceTests
{
    private const int CallerId = 1;

    private readonly FakeUserRepository _users = new();
    private readonly FakeSessionRepository _sessions = new();
    private readonly FakeReferenceLookup _lookup = new();
    private readonly FakePasswordPolicyRepository _policies;
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);
    private readonly UserService _service;

    public UserServiceTests()
    {
        // Role 1: HARD-like policy (12+ chars, all classes, expiry 90 days, history 5).
        _policies = new FakePasswordPolicyRepository(TestData.Policy(
            id: 3, name: "HARD", minLength: 12, upper: true, lower: true, number: true, special: true,
            historyCount: 5, expiryEnabled: true, expiryDays: 90));

        // Role 2: EASY-like policy without expiry.
        _lookup.Roles.Add(2);
        _policies.ByRole[2] = TestData.Policy(id: 1, name: "EASY", minLength: 6, number: true);

        // Role 3 exists but has no policy assigned.
        _lookup.Roles.Add(3);

        var hasher = new FakePasswordHasher();
        var rules = new PasswordRules(_policies, _users, new PasswordPolicyValidator(hasher));

        _service = new UserService(_users, _sessions, _lookup, rules, hasher, _clock, new FakeCurrentUser(userId: CallerId), _log,
            new CreateUserRequestValidator(), new UpdateUserRequestValidator());
    }

    private static CreateUserRequest ValidCreate(string userName = "bob") => new()
    {
        UserName = userName,
        Password = "Str0ng!Passw0rd#",
        RoleId = 1,
        CompanyId = 1,
        FullName = "Bob Builder",
        Email = "bob@example.com",
    };

    private static string Json(object? value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    // ---------- create ----------

    [Fact]
    public async Task Create_HappyPath_SavesHashedUser_SetsExpiry_AuditsInSecondCommit()
    {
        var response = await _service.CreateAsync(ValidCreate(), CancellationToken.None);

        response.Id.Should().BeGreaterThan(0);
        response.UserName.Should().Be("bob");
        response.ForcePasswordChange.Should().BeTrue();                       // default for admin-created users
        response.PasswordExpiresAt.Should().Be(TestData.Now.AddDays(90));     // from the HARD policy
        response.IsActive.Should().BeTrue();

        var stored = _users.Users.Single();
        stored.PasswordHash.Should().Be("H:Str0ng!Passw0rd#");                // hashed, never plain
        stored.CreatedBy.Should().Be(CallerId);
        stored.PasswordHistory.Should().HaveCount(1);
        _users.SaveCount.Should().Be(2);                                      // user first (needs Id), then its audit row

        var entry = _log.Entries.Single(e => e.ActionType == UserLogActions.UserCreated);
        entry.EntityId.Should().Be(stored.Id.ToString());
        Json(entry.NewValue).Should().NotContainAny("hash", "Hash", "H:Str0ng");
    }

    [Fact]
    public async Task Create_NoForcedChange_PolicyWithoutExpiry_LeavesExpiryNull()
    {
        var request = ValidCreate();
        request.RoleId = 2;
        request.Password = "easy123";
        request.ForcePasswordChange = false;

        var response = await _service.CreateAsync(request, CancellationToken.None);

        response.ForcePasswordChange.Should().BeFalse();
        response.PasswordExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithoutCompany_SkipsCompanyLookup()
    {
        var request = ValidCreate();
        request.CompanyId = null;

        await _service.CreateAsync(request, CancellationToken.None);

        _lookup.CompanyChecks.Should().Be(0);
        _users.Users.Single().CompanyId.Should().BeNull();
    }

    [Fact]
    public async Task Create_DuplicateUserName_IgnoringCase_Returns409()
    {
        _users.Users.Add(TestData.User(userName: "Bob"));

        var act = () => _service.CreateAsync(ValidCreate("bob"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should().Be(ErrorCodes.UserNameTaken);
        _log.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_UnknownRole_Returns404()
    {
        var request = ValidCreate();
        request.RoleId = 42;

        await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_UnknownCompany_Returns404()
    {
        var request = ValidCreate();
        request.CompanyId = 42;

        await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_RoleWithoutPolicy_Returns409()
    {
        var request = ValidCreate();
        request.RoleId = 3;                                                   // exists, but no policy mapped

        var ex = await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.PasswordPolicyNotConfigured);
    }

    [Fact]
    public async Task Create_PasswordBreaksPolicy_Returns400OnPasswordField_SavesNothing()
    {
        var request = ValidCreate();
        request.Password = "short";

        var ex = await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().ContainKey("password");
        ex.Which.Errors["password"].Should().NotBeEmpty();
        _users.Users.Should().BeEmpty();
        _log.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_InvalidShape_Returns400OnField()
    {
        var request = ValidCreate();
        request.UserName = "has spaces";
        request.Email = "not-an-email";

        var ex = await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Keys.Should().Contain("userName").And.Contain("email");
    }

    // ---------- read ----------

    [Fact]
    public async Task GetById_Unknown_Returns404()
    {
        await _service.Invoking(s => s.GetByIdAsync(999, CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_SerialisedResponse_ContainsNoSecret()
    {
        _users.Users.Add(TestData.User(id: 5, passwordHash: "H:TopSecret"));

        var response = await _service.GetByIdAsync(5, CancellationToken.None);

        var json = Json(response);
        json.Should().Contain("\"userName\":\"alice\"");
        json.Should().NotContainAny("passwordHash", "TopSecret", "H:");
    }

    [Theory]
    [InlineData(0, 0, 1, 50)]        // defaults
    [InlineData(2, 200, 2, 200)]     // exactly the cap
    [InlineData(2, 201, 2, 200)]     // one over the cap -> capped
    [InlineData(-3, 10, 1, 10)]      // negative page -> 1
    public async Task GetPage_ClampsPaging(int page, int pageSize, int expectedPage, int expectedSize)
    {
        await _service.GetPageAsync(new UserListRequest { Page = page, PageSize = pageSize, Search = " bo " }, CancellationToken.None);

        _users.LastPageQuery.Should().Be(("bo", expectedPage, expectedSize));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPage_BlankSearch_BecomesNull(string? search)
    {
        await _service.GetPageAsync(new UserListRequest { Search = search }, CancellationToken.None);

        _users.LastPageQuery!.Value.Search.Should().BeNull();
    }

    // ---------- update ----------

    [Fact]
    public async Task Update_ChangesProfile_AuditsOldAndNew_InOneCommit()
    {
        _users.Users.Add(TestData.User(id: 5));

        var response = await _service.UpdateAsync(5, new UpdateUserRequest { RoleId = 1, FullName = "Alice B", Email = "ab@x.com" }, CancellationToken.None);

        response.FullName.Should().Be("Alice B");
        _users.SaveCount.Should().Be(1);
        var entry = _log.Entries.Single(e => e.ActionType == UserLogActions.UserUpdated);
        ((UserResponse)entry.OldValue!).FullName.Should().Be("Alice");
        ((UserResponse)entry.NewValue!).FullName.Should().Be("Alice B");
        Json(entry.OldValue).Should().NotContain("H:");
    }

    [Fact]
    public async Task Update_UnknownUser_Returns404_NoAudit()
    {
        await _service.Invoking(s => s.UpdateAsync(999, new UpdateUserRequest { RoleId = 1 }, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();

        _log.Entries.Should().BeEmpty();
        _users.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_UnknownRole_Returns404_SavesNothing()
    {
        _users.Users.Add(TestData.User(id: 5));

        await _service.Invoking(s => s.UpdateAsync(5, new UpdateUserRequest { RoleId = 42 }, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();

        _users.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_ToRoleWithoutPolicy_Returns409()
    {
        _users.Users.Add(TestData.User(id: 5, roleId: 1));

        var ex = await _service.Invoking(s => s.UpdateAsync(5, new UpdateUserRequest { RoleId = 3 }, CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.PasswordPolicyNotConfigured);
        _users.Users.Single().RoleId.Should().Be(1);
    }

    [Fact]
    public async Task Update_OwnRole_Returns409()
    {
        _users.Users.Add(TestData.User(id: CallerId, roleId: 1));

        var ex = await _service.Invoking(s => s.UpdateAsync(CallerId, new UpdateUserRequest { RoleId = 2 }, CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.CannotChangeOwnRole);
    }

    // ---------- lifecycle ----------

    [Fact]
    public async Task Deactivate_ThenActivate_UpdatesStateAndAudits()
    {
        _users.Users.Add(TestData.User(id: 5));

        var deactivated = await _service.DeactivateAsync(5, CancellationToken.None);
        _users.Users.Single().IsActive.Should().BeFalse();
        deactivated.IsActive.Should().BeFalse();
        _log.Has(UserLogActions.UserDeactivated).Should().BeTrue();

        var activated = await _service.ActivateAsync(5, CancellationToken.None);
        _users.Users.Single().IsActive.Should().BeTrue();
        activated.IsActive.Should().BeTrue();
        _log.Has(UserLogActions.UserActivated).Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_Self_Returns409_ChangesNothing()
    {
        _users.Users.Add(TestData.User(id: CallerId));

        var ex = await _service.Invoking(s => s.DeactivateAsync(CallerId, CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.CannotDeactivateSelf);
        _users.Users.Single().IsActive.Should().BeTrue();
        _log.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Deactivate_RevokesEveryActiveSession()
    {
        _users.Users.Add(TestData.User(id: 5));
        var live = USER_SESSION.Create(5, "TH:a", TestData.Now, 60, TestData.Now.AddHours(1), null, null);
        var ended = USER_SESSION.Create(5, "TH:b", TestData.Now, 60, TestData.Now.AddHours(1), null, null);
        ended.Logout(TestData.Now);
        var someoneElse = USER_SESSION.Create(6, "TH:c", TestData.Now, 60, TestData.Now.AddHours(1), null, null);
        await _sessions.AddAsync(live, CancellationToken.None);
        await _sessions.AddAsync(ended, CancellationToken.None);
        await _sessions.AddAsync(someoneElse, CancellationToken.None);

        await _service.DeactivateAsync(5, CancellationToken.None);

        live.Status.Should().Be(USER_SESSION.StatusRevoked);
        ended.Status.Should().Be(USER_SESSION.StatusLoggedOut);   // untouched
        someoneElse.Status.Should().Be(USER_SESSION.StatusActive); // untouched
    }

    [Fact]
    public async Task Unlock_ClearsLock_Audits()
    {
        var user = TestData.User(id: 5);
        user.RegisterFailedLogin(TestData.Now, true, 1, 1440);
        user.LockedUntil.Should().NotBeNull();
        _users.Users.Add(user);

        var response = await _service.UnlockAsync(5, CancellationToken.None);

        user.LockedUntil.Should().BeNull();
        user.FailedLoginAttempts.Should().Be(0);
        response.LockedUntil.Should().BeNull();                // the caller sees the new state, not an empty 204
        response.FailedLoginAttempts.Should().Be(0);
        response.IsBlocked.Should().BeFalse();
        _log.Has(UserLogActions.AccountUnlocked).Should().BeTrue();
    }
}
