using FluentAssertions;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Rules;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Users;

/// <summary>The "who may give what" rules on the user-access screens (Agent Manager vs admin users).</summary>
public sealed class UserAccessServiceTests
{
    private const int AgentManager = 1;      // the caller: may manage users, but not admin users
    private const int Company = 1;
    private const int OwnSupplierCode = 10;

    private const int OperatorRole = 2;
    private const int PlantAdminRole = 4;
    private const int SuperAdminRole = 99;

    private const int UserUnlock = 1;
    private const int SecurityEdit = 2;      // ADMIN scope
    private const int SupplierAdd = 3;       // SYSTEM scope

    private readonly FakeUserRepository _users = new();
    private readonly FakeUserAccessRepository _userAccess = new();
    private readonly FakePasswordPolicyRepository _policies = new(TestData.Policy());
    private readonly FakeAccessRepository _access = new();
    private readonly FakeTenantContext _tenant = new() { CompanyId = Company, SupplierCodeId = OwnSupplierCode, ExciseCode = "RJ" };
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);

    public UserAccessServiceTests()
    {
        _policies.ByRole[OperatorRole] = TestData.Policy();
        _policies.ByRole[PlantAdminRole] = TestData.Policy();
        _policies.ByRole[SuperAdminRole] = TestData.Policy();

        _userAccess.Roles.Add(AccessRows.Role(OperatorRole, Company, "Operator"));
        _userAccess.Roles.Add(AccessRows.Role(PlantAdminRole, Company, "Plant Admin", isAdminRole: true));
        _userAccess.Roles.Add(AccessRows.Role(SuperAdminRole, null, "Super Admin", isSystem: true));
        _userAccess.Actions.Add(AccessRows.Action(UserUnlock, "user.unlock"));
        _userAccess.Actions.Add(AccessRows.Action(SecurityEdit, "securityconfig.edit", GrantScope.ADMIN));
        _userAccess.Actions.Add(AccessRows.Action(SupplierAdd, "suppliercode.add", GrantScope.SYSTEM));
        _userAccess.SupplierCodeCompany[OwnSupplierCode] = Company;
        _userAccess.SupplierCodeCompany[20] = 2;

        AddUser(AgentManager, OperatorRole);
        AddUser(5, OperatorRole);              // an ordinary user
        AddUser(6, PlantAdminRole);            // an admin user
    }

    private UserAccessService Service(int callerId = AgentManager)
    {
        var current = new CurrentAccess(_access, new FakeCurrentUser(callerId), _tenant);
        return new UserAccessService(_userAccess, new UserAccessRules(_users, _userAccess, current),
            new PasswordRules(_policies, _users, new PasswordPolicyValidator(new FakePasswordHasher()), _clock),
            current, _clock, _log, new UpdateUserRolesRequestValidator(), new UpdateUserRightsRequestValidator());
    }

    private void AddUser(int id, int role)
    {
        _users.Users.Add(TestData.User(id: id, userName: "user" + id, companyId: Company));
        _userAccess.UserRoles[id] = new() { new UserRoleAssignment { RoleId = role } };
    }

    private static UpdateUserRolesRequest Roles(params (int Role, int? OwnSupplierCode)[] roles) =>
        new() { Roles = roles.Select(r => new UserRoleAssignment { RoleId = r.Role, SupplierCodeId = r.OwnSupplierCode }).ToList() };

    private static UpdateUserRightsRequest Rights(params (int Action, int? OwnSupplierCode)[] rights) =>
        new() { Rights = rights.Select(r => new UserRightAssignment { PageActionId = r.Action, SupplierCodeId = r.OwnSupplierCode }).ToList() };

    // ---------- roles ----------

    [Fact]
    public async Task UpdateRoles_OrdinaryUser_ReplacesRoles_Audited()
    {
        var response = await Service().UpdateRolesAsync(5, Roles((OperatorRole, OwnSupplierCode)), CancellationToken.None);

        response.Roles.Should().ContainSingle(r => r.RoleId == OperatorRole && r.SupplierCodeId == OwnSupplierCode);
        _log.Has(UserLogActions.UserRolesChanged).Should().BeTrue();
        _userAccess.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRoles_Own_Returns409()
    {
        var ex = await Service().Invoking(s => s.UpdateRolesAsync(AgentManager, Roles((OperatorRole, null)), CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.CannotChangeOwnAccess);
    }

    [Fact]
    public async Task UpdateRoles_Own_AllowedForSuperAdmin()
    {
        _access.SuperAdmins.Add(AgentManager);

        await Service().UpdateRolesAsync(AgentManager, Roles((OperatorRole, null)), CancellationToken.None);
    }

    [Fact]
    public async Task UpdateRoles_GivingAdminRole_WithoutManageAdmin_Returns403()
    {
        var ex = await Service().Invoking(s => s.UpdateRolesAsync(5, Roles((PlantAdminRole, null)), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.AdminUserProtected);
        _userAccess.UserRoles[5].Single().RoleId.Should().Be(OperatorRole);
    }

    [Fact]
    public async Task UpdateRoles_TakingRolesFromAnAdminUser_WithoutManageAdmin_Returns403()
    {
        await Service().Invoking(s => s.UpdateRolesAsync(6, Roles((OperatorRole, null)), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateRoles_SuperAdminRole_ByCompanyUser_Returns403()
    {
        _access.Grant(AgentManager, OwnSupplierCode, Permissions.UserManageAdmin);

        await Service().Invoking(s => s.UpdateRolesAsync(5, Roles((SuperAdminRole, null)), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateRoles_SupplierCodeOfAnotherCompany_Returns404()
    {
        await Service().Invoking(s => s.UpdateRolesAsync(5, Roles((OperatorRole, 20)), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ---------- custom rights ----------

    [Fact]
    public async Task UpdateRights_OrdinaryRight_ForOneSupplierCode()
    {
        var response = await Service().UpdateRightsAsync(5, Rights((UserUnlock, OwnSupplierCode)), CancellationToken.None);

        response.Rights.Should().ContainSingle(r => r.PageActionId == UserUnlock && r.SupplierCodeId == OwnSupplierCode);
        _log.Has(UserLogActions.UserRightsChanged).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRights_SystemRight_Returns403NotGrantable()
    {
        var ex = await Service().Invoking(s => s.UpdateRightsAsync(5, Rights((SupplierAdd, null)), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.RightNotGrantable);
    }

    [Fact]
    public async Task UpdateRights_AdminRight_WithoutManageAdmin_Returns403()
    {
        var ex = await Service().Invoking(s => s.UpdateRightsAsync(5, Rights((SecurityEdit, null)), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.AdminUserProtected);
    }

    [Fact]
    public async Task UpdateRights_AdminRightAlreadyHeld_UnchangedSaveIsAllowed()
    {
        _userAccess.UserRights[5] = new() { new UserRightAssignment { PageActionId = SecurityEdit } };

        var response = await Service().UpdateRightsAsync(5, Rights((SecurityEdit, null), (UserUnlock, OwnSupplierCode)), CancellationToken.None);

        response.Rights.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateRights_UnknownAction_Returns400()
    {
        var ex = await Service().Invoking(s => s.UpdateRightsAsync(5, Rights((999, null)), CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().ContainKey("rights");
    }

    [Fact]
    public async Task Get_UserOfAnotherCompany_Returns404()
    {
        _users.Users.Add(TestData.User(id: 7, userName: "stranger", companyId: 2));

        await Service().Invoking(s => s.GetAsync(7, CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }
}
