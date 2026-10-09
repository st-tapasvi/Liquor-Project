using FluentAssertions;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.RoleGroups;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.RoleGroups;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.RoleGroups;

/// <summary>Role groups: bundles of a company's roles; who may change them and what a change does.</summary>
public sealed class RoleGroupServiceTests
{
    private const int CallerId = 1;
    private const int Company = 1;
    private const int OwnSupplierCode = 10;

    private const int Operator772 = 20;
    private const int Operator1028 = 21;
    private const int PlantAdmin = 22;
    private const int ForeignRole = 23;
    private const int NoPolicyRole = 24;

    private readonly FakeRoleGroupRepository _groups = new();
    private readonly FakeAccessRepository _access = new();
    private readonly FakePasswordPolicyRepository _policies = new(TestData.Policy());
    private readonly FakeTenantContext _tenant = new() { CompanyId = Company, SupplierCodeId = OwnSupplierCode, ExciseCode = "RJ" };
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);

    public RoleGroupServiceTests()
    {
        _groups.Roles.Add(AccessRows.Role(Operator772, Company, "Operator", supplierCodeId: 10));
        _groups.Roles.Add(AccessRows.Role(Operator1028, Company, "Operator", supplierCodeId: 11));
        _groups.Roles.Add(AccessRows.Role(PlantAdmin, Company, "Plant Admin", isAdminRole: true));
        _groups.Roles.Add(AccessRows.Role(ForeignRole, 2, "Operator", supplierCodeId: 30));
        _groups.Roles.Add(AccessRows.Role(NoPolicyRole, Company, "Viewer", supplierCodeId: 10));
        foreach (var id in new[] { Operator772, Operator1028, PlantAdmin, ForeignRole })
        {
            _policies.ByRole[id] = TestData.Policy();
        }
    }

    private RoleGroupService Service() =>
        new(_groups, new CurrentAccess(_access, new FakeCurrentUser(CallerId), _tenant),
            new PasswordRules(_policies, new FakeUserRepository(), new PasswordPolicyValidator(new FakePasswordHasher()), _clock),
            _clock, _log, new SaveRoleGroupRequestValidator(), new UpdateRoleGroupRolesRequestValidator());

    private ROLE_GROUP AddGroup(int id, string name = "All Operators", int companyId = Company, params int[] roles)
    {
        var group = ROLE_GROUP.Create(companyId, name, null, TestData.Now, null).WithId(id);
        _groups.Groups.Add(group);
        _groups.GroupRoles[id] = roles.ToHashSet();
        return group;
    }

    private static UpdateRoleGroupRolesRequest Roles(params int[] ids) => new() { RoleIds = ids.ToList() };

    // ---------- create / list ----------

    [Fact]
    public async Task Create_InCallersCompany_Audited()
    {
        var response = await Service().CreateAsync(new SaveRoleGroupRequest { GroupName = " All Operators ", Description = "Line staff" }, CancellationToken.None);

        response.CompanyId.Should().Be(Company);
        response.GroupName.Should().Be("All Operators");
        response.Roles.Should().BeEmpty();
        _log.Has(UserLogActions.RoleGroupCreated).Should().BeTrue();
    }

    [Fact]
    public async Task Create_SameName_Returns409()
    {
        AddGroup(1);

        var ex = await Service().Invoking(s => s.CreateAsync(new SaveRoleGroupRequest { GroupName = "all operators" }, CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.RoleGroupNameTaken);
    }

    [Fact]
    public async Task GetList_OwnCompanyOnly()
    {
        AddGroup(1);
        AddGroup(2, "Other", companyId: 2);

        (await Service().GetListAsync(CancellationToken.None)).Select(g => g.Id).Should().Equal(1);
    }

    [Fact]
    public async Task GetById_AnotherCompanysGroup_Returns404()
    {
        AddGroup(2, "Other", companyId: 2);

        await Service().Invoking(s => s.GetByIdAsync(2, CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    // ---------- roles of a group ----------

    [Fact]
    public async Task UpdateRoles_NewSupplierCodesOperator_IsAddedOnceForEveryUserOfTheGroup()
    {
        AddGroup(1, roles: Operator772);
        _groups.Members[1] = new() { 5, 6, 7 };

        var response = await Service().UpdateRolesAsync(1, Roles(Operator772, Operator1028), CancellationToken.None);

        response.Roles.Select(r => r.RoleId).Should().BeEquivalentTo(new[] { Operator772, Operator1028 });
        response.UserCount.Should().Be(3);
        _log.Entries.Should().ContainSingle(e => e.ActionType == UserLogActions.RoleGroupRolesChanged && e.Description!.Contains("3 user(s)"));
    }

    [Theory]
    [InlineData(ForeignRole)]   // another company's role
    [InlineData(999)]           // unknown
    public async Task UpdateRoles_RoleNotOfTheCompany_Returns404(int roleId)
    {
        AddGroup(1);

        await Service().Invoking(s => s.UpdateRolesAsync(1, Roles(roleId), CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateRoles_RoleWithoutPasswordPolicy_Returns409()
    {
        AddGroup(1);

        var ex = await Service().Invoking(s => s.UpdateRolesAsync(1, Roles(NoPolicyRole), CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.PasswordPolicyNotConfigured);
    }

    [Fact]
    public async Task UpdateRoles_PuttingInAnAdminRole_NeedsManageAdmin()
    {
        AddGroup(1, roles: Operator772);

        var ex = await Service().Invoking(s => s.UpdateRolesAsync(1, Roles(Operator772, PlantAdmin), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.AdminUserProtected);

        _access.Grant(CallerId, OwnSupplierCode, Permissions.UserManageAdmin);
        (await Service().UpdateRolesAsync(1, Roles(Operator772, PlantAdmin), CancellationToken.None)).HasAdminRole.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRoles_GroupHeldByAnAdminUser_NeedsManageAdmin()
    {
        AddGroup(1, roles: Operator772);
        _groups.GroupsWithAdminMember.Add(1);

        await Service().Invoking(s => s.UpdateRolesAsync(1, Roles(Operator772, Operator1028), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateRoles_GroupTheCallerHolds_Returns409_ExceptForAdmin()
    {
        AddGroup(1, roles: Operator772);
        _groups.Members[1] = new() { CallerId };

        var ex = await Service().Invoking(s => s.UpdateRolesAsync(1, Roles(Operator772, Operator1028), CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.CannotChangeOwnAccess);

        _access.SuperAdmins.Add(CallerId);
        await Service().UpdateRolesAsync(1, Roles(Operator772, Operator1028), CancellationToken.None);
    }

    // ---------- delete ----------

    [Fact]
    public async Task Delete_GroupGivenToUsers_Returns409()
    {
        AddGroup(1, roles: Operator772);
        _groups.Members[1] = new() { 5 };

        var ex = await Service().Invoking(s => s.DeleteAsync(1, CancellationToken.None)).Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.RoleGroupInUse);
        _groups.Groups.Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_UnusedGroup_RemovesIt_Audited()
    {
        AddGroup(1, roles: Operator772);

        await Service().DeleteAsync(1, CancellationToken.None);

        _groups.Groups.Should().BeEmpty();
        _log.Has(UserLogActions.RoleGroupDeleted).Should().BeTrue();
    }
}
