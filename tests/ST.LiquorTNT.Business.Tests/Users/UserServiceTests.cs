using System.Text.Json;
using FluentAssertions;
using ST.LiquorTNT.Business.Access;
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
    private const int Company = 1;
    private const int OtherCompany = 2;
    private const int OwnSupplierCode = 10;          // RJ CL 550 of company 1
    private const int OtherSupplierCode = 20;     // a supplierCode of company 2

    // roles of company 1 (and one of company 2)
    private const int PlantManagerRole = 1;  // HARD policy
    private const int OperatorRole = 2;      // EASY policy
    private const int NoPolicyRole = 3;      // exists, no policy
    private const int PlantAdminRole = 4;    // admin role, HARD policy
    private const int OtherCompanyRole = 5;

    private readonly FakeUserRepository _users = new();
    private readonly FakeUserAccessRepository _userAccess = new();
    private readonly FakeSessionRepository _sessions = new();
    private readonly FakeReferenceLookup _lookup = new();
    private readonly FakePasswordPolicyRepository _policies;
    private readonly FakeAccessRepository _access = new();
    private readonly FakeTenantContext _tenant = new() { CompanyId = Company, SupplierCodeId = OwnSupplierCode, ExciseCode = "RJ" };
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);
    private readonly UserService _service;

    public UserServiceTests()
    {
        var hard = TestData.Policy(id: 3, name: "HARD", minLength: 12, upper: true, lower: true, number: true, special: true,
            historyCount: 5, expiryEnabled: true, expiryDays: 90);
        _policies = new FakePasswordPolicyRepository(hard);
        _policies.ByRole[PlantManagerRole] = hard;
        _policies.ByRole[OperatorRole] = TestData.Policy(id: 1, name: "EASY", minLength: 6, number: true);
        _policies.ByRole[PlantAdminRole] = hard;

        _userAccess.Roles.Add(AccessRows.Role(PlantManagerRole, Company, "Plant Manager", supplierCodeId: OwnSupplierCode));
        _userAccess.Roles.Add(AccessRows.Role(OperatorRole, Company, "Operator", supplierCodeId: OwnSupplierCode));
        _userAccess.Roles.Add(AccessRows.Role(NoPolicyRole, Company, "Viewer"));
        _userAccess.Roles.Add(AccessRows.Role(PlantAdminRole, Company, "Plant Admin", isAdminRole: true));
        _userAccess.Roles.Add(AccessRows.Role(OtherCompanyRole, OtherCompany, "Operator"));
        _userAccess.SupplierCodeCompany[OwnSupplierCode] = Company;
        _userAccess.SupplierCodeCompany[OtherSupplierCode] = OtherCompany;

        var hasher = new FakePasswordHasher();
        var current = new CurrentAccess(_access, new FakeCurrentUser(userId: CallerId), _tenant);
        var rules = new UserAccessRules(_users, _userAccess, current);
        var passwordRules = new PasswordRules(_policies, _users, new PasswordPolicyValidator(hasher), _clock);

        _service = new UserService(_users, _userAccess, _sessions, _lookup, rules, current, passwordRules, hasher, _clock, _log,
            new CreateUserRequestValidator(), new UpdateUserRequestValidator());
    }

    private static CreateUserRequest ValidCreate(string userName = "bob", int roleId = PlantManagerRole) => new()
    {
        UserName = userName,
        Password = "Str0ng!Passw0rd#",
        Roles = new() { new UserRoleAssignment { RoleId = roleId } },
        FullName = "Bob Builder",
        Email = "bob@example.com",
    };

    private USERS AddUser(int id, int? companyId = Company, params int[] roles)
    {
        var user = TestData.User(id: id, userName: "user" + id, companyId: companyId);
        _users.Users.Add(user);
        _userAccess.UserRoles[id] = roles.Select(r => new UserRoleAssignment { RoleId = r }).ToList();
        return user;
    }

    private static string Json(object? value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    // ---------- create ----------

    [Fact]
    public async Task Create_HappyPath_SavesHashedUser_InCallersCompany_WithRoles_AuditsInSecondCommit()
    {
        var response = await _service.CreateAsync(ValidCreate(), CancellationToken.None);

        response.UserName.Should().Be("bob");
        response.CompanyId.Should().Be(Company);
        response.ForcePasswordChange.Should().BeTrue();                       // default for admin-created users
        response.PasswordExpiresAt.Should().Be(TestData.Now.AddDays(90));     // from the HARD policy

        var stored = _users.Users.Single();
        stored.PasswordHash.Should().Be("H:Str0ng!Passw0rd#");                // hashed, never plain
        stored.CreatedBy.Should().Be(CallerId);
        _userAccess.UserRoles[stored.Id].Should().ContainSingle(r => r.RoleId == PlantManagerRole);
        _users.SaveCount.Should().Be(2);                                      // user first (needs Id), then roles + audit

        var entry = _log.Entries.Single(e => e.ActionType == UserLogActions.UserCreated);
        entry.EntityId.Should().Be(stored.Id.ToString());
        Json(entry.NewValue).Should().NotContainAny("hash", "Hash", "H:Str0ng");
    }

    [Fact]
    public async Task Create_CompanyUser_CannotPlaceUserInAnotherCompany()
    {
        var request = ValidCreate();
        request.CompanyId = OtherCompany;                                     // ignored for a company user

        var response = await _service.CreateAsync(request, CancellationToken.None);

        response.CompanyId.Should().Be(Company);
    }

    [Fact]
    public async Task Create_NoForcedChange_PolicyWithoutExpiry_LeavesExpiryNull()
    {
        var request = ValidCreate(roleId: OperatorRole);
        request.Password = "easy123";
        request.ForcePasswordChange = false;

        var response = await _service.CreateAsync(request, CancellationToken.None);

        response.ForcePasswordChange.Should().BeFalse();
        response.PasswordExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Create_SeveralRoles_PasswordMustMeetTheStrictestPolicy()
    {
        var request = ValidCreate(roleId: OperatorRole);
        request.Roles.Add(new UserRoleAssignment { RoleId = PlantManagerRole });
        request.Password = "easy123";                                         // fine for EASY, too weak for HARD

        var ex = await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().ContainKey("password");
        _users.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateUserName_IgnoringCase_Returns409()
    {
        _users.Users.Add(TestData.User(userName: "Bob"));

        var act = () => _service.CreateAsync(ValidCreate("bob"), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should().Be(ErrorCodes.UserNameTaken);
        _log.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(42)]                    // unknown
    [InlineData(OtherCompanyRole)]      // another company's role
    public async Task Create_RoleNotOfThisCompany_Returns404(int roleId)
    {
        await _service.Invoking(s => s.CreateAsync(ValidCreate(roleId: roleId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_RoleOfADeactivatedSupplierCode_Returns404()
    {
        const int retiredSupplierCode = 30;                                   // not active: missing from SupplierCodeCompany
        _userAccess.Roles.Add(AccessRows.Role(6, Company, "Operator", supplierCodeId: retiredSupplierCode));
        _policies.ByRole[6] = _policies.ByRole[OperatorRole];

        await _service.Invoking(s => s.CreateAsync(ValidCreate(roleId: 6), CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_RoleWithoutPolicy_Returns409()
    {
        var ex = await _service.Invoking(s => s.CreateAsync(ValidCreate(roleId: NoPolicyRole), CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.PasswordPolicyNotConfigured);
    }

    [Fact]
    public async Task Create_AdminRole_WithoutManageAdmin_Returns403()
    {
        var ex = await _service.Invoking(s => s.CreateAsync(ValidCreate(roleId: PlantAdminRole), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.AdminUserProtected);
        _users.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_AdminRole_WithManageAdmin_Succeeds()
    {
        _access.Grant(CallerId, OwnSupplierCode, Permissions.UserManageAdmin);

        var response = await _service.CreateAsync(ValidCreate(roleId: PlantAdminRole), CancellationToken.None);

        response.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_SuperAdmin_MayNameTheCompany_UnknownCompanyIs404()
    {
        _access.SuperAdmins.Add(CallerId);
        var request = ValidCreate();
        request.CompanyId = 42;

        await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_NoRoles_Returns400()
    {
        var request = ValidCreate();
        request.Roles.Clear();

        var ex = await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().ContainKey("roles");
    }

    [Fact]
    public async Task Create_PasswordBreaksPolicy_Returns400OnPasswordField_SavesNothing()
    {
        var request = ValidCreate();
        request.Password = "short";

        var ex = await _service.Invoking(s => s.CreateAsync(request, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

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
    public async Task GetById_UserOfAnotherCompany_Returns404()
    {
        AddUser(5, companyId: OtherCompany);

        await _service.Invoking(s => s.GetByIdAsync(5, CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetById_SerialisedResponse_ContainsNoSecret()
    {
        _users.Users.Add(TestData.User(id: 5, passwordHash: "H:TopSecret", companyId: Company));

        var json = Json(await _service.GetByIdAsync(5, CancellationToken.None));

        json.Should().Contain("\"userName\":\"alice\"");
        json.Should().NotContainAny("passwordHash", "TopSecret", "H:");
    }

    [Theory]
    [InlineData(0, 0, 1, 50)]        // defaults
    [InlineData(2, 200, 2, 200)]     // exactly the cap
    [InlineData(2, 201, 2, 200)]     // one over the cap -> capped
    [InlineData(-3, 10, 1, 10)]      // negative page -> 1
    public async Task GetPage_ClampsPaging_LimitsToCallersCompany(int page, int pageSize, int expectedPage, int expectedSize)
    {
        await _service.GetPageAsync(new UserListRequest { Page = page, PageSize = pageSize, Search = " bo " }, CancellationToken.None);

        _users.LastPageQuery.Should().Be(((int?)Company, (string?)"bo", expectedPage, expectedSize));
    }

    [Fact]
    public async Task GetPage_SuperAdminWithoutSupplierCode_SeesEveryCompany()
    {
        _access.SuperAdmins.Add(CallerId);
        _tenant.SupplierCodeId = null;
        _tenant.CompanyId = null;

        await _service.GetPageAsync(new UserListRequest(), CancellationToken.None);

        _users.LastPageQuery!.Value.CompanyId.Should().BeNull();
    }

    // ---------- update ----------

    [Fact]
    public async Task Update_ChangesProfile_AuditsOldAndNew_InOneCommit()
    {
        AddUser(5, Company, OperatorRole);

        var response = await _service.UpdateAsync(5, new UpdateUserRequest { FullName = "Alice B", Email = "ab@x.com" }, CancellationToken.None);

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
        await _service.Invoking(s => s.UpdateAsync(999, new UpdateUserRequest(), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();

        _log.Entries.Should().BeEmpty();
        _users.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_AdminUser_WithoutManageAdmin_Returns403()
    {
        AddUser(5, Company, PlantAdminRole);

        var ex = await _service.Invoking(s => s.UpdateAsync(5, new UpdateUserRequest { FullName = "x" }, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.AdminUserProtected);
        _users.SaveCount.Should().Be(0);
    }

    // ---------- lifecycle ----------

    [Fact]
    public async Task Deactivate_ThenActivate_UpdatesStateAndAudits()
    {
        AddUser(5, Company, OperatorRole);

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
        AddUser(CallerId, Company, OperatorRole);

        var ex = await _service.Invoking(s => s.DeactivateAsync(CallerId, CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.CannotDeactivateSelf);
        _users.Users.Single().IsActive.Should().BeTrue();
        _log.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Deactivate_AdminUser_WithoutManageAdmin_Returns403()
    {
        AddUser(5, Company, PlantAdminRole);

        await _service.Invoking(s => s.DeactivateAsync(5, CancellationToken.None)).Should().ThrowAsync<ForbiddenException>();
        _users.Users.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_RevokesEveryActiveSession()
    {
        AddUser(5, Company, OperatorRole);
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
        var user = AddUser(5, Company, OperatorRole);
        user.RegisterFailedLogin(TestData.Now, true, 1, 1440);
        user.LockedUntil.Should().NotBeNull();

        var response = await _service.UnlockAsync(5, CancellationToken.None);

        user.LockedUntil.Should().BeNull();
        user.FailedLoginAttempts.Should().Be(0);
        response.LockedUntil.Should().BeNull();                // the caller sees the new state, not an empty 204
        response.FailedLoginAttempts.Should().Be(0);
        response.IsBlocked.Should().BeFalse();
        _log.Has(UserLogActions.AccountUnlocked).Should().BeTrue();
    }
}
