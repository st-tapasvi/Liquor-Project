using FluentAssertions;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Contracts.Roles;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Domain.Rules;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Roles;

public sealed class RoleServiceTests
{
    private const int CallerId = 1;
    private const int Company = 1;
    private const int OwnSupplierCode = 10;

    // page actions
    private const int RoleView = 1;
    private const int UserUnlock = 2;
    private const int SecurityEdit = 3;      // ADMIN scope
    private const int SupplierAdd = 4;       // SYSTEM scope

    private readonly FakeRoleRepository _roles = new();
    private readonly FakeAccessRepository _access = new();
    private readonly FakeTenantContext _tenant = new() { CompanyId = Company, SupplierCodeId = OwnSupplierCode, ExciseCode = "RJ" };
    private readonly FakeUserLogWriter _log = new();
    private readonly FixedClock _clock = new(TestData.Now);
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        _roles.Roles.Add(AccessRows.Role(1, null, "Super Admin", isSystem: true));
        _roles.Roles.Add(AccessRows.Role(2, null, "Operator", isTemplate: true));
        _roles.Roles.Add(AccessRows.Role(10, Company, "Operator"));
        _roles.Roles.Add(AccessRows.Role(11, Company, "Plant Admin", isAdminRole: true));
        _roles.Roles.Add(AccessRows.Role(20, 2, "Operator"));                 // another company's
        _roles.Actions.Add(AccessRows.Action(RoleView, "role.view"));
        _roles.Actions.Add(AccessRows.Action(UserUnlock, "user.unlock"));
        _roles.Actions.Add(AccessRows.Action(SecurityEdit, "securityconfig.edit", GrantScope.ADMIN));
        _roles.Actions.Add(AccessRows.Action(SupplierAdd, "suppliercode.add", GrantScope.SYSTEM));

        _service = new RoleService(_roles, new CurrentAccess(_access, new FakeCurrentUser(CallerId), _tenant), _clock, _log,
            new SaveRoleRequestValidator(), new UpdateRoleRightsRequestValidator());
    }

    private static SaveRoleRequest Save(string name = "Packer", bool admin = false, int policy = 2) =>
        new() { RoleName = name, PasswordPolicyId = policy, IsAdminRole = admin };

    private static UpdateRoleRightsRequest Rights(params int[] ids) => new() { PageActionIds = ids.ToList() };

    // ---------- list / read ----------

    [Fact]
    public async Task GetList_CompanyUser_SeesOwnCompanyOnly()
    {
        var list = await _service.GetListAsync(CancellationToken.None);

        list.Select(r => r.Id).Should().BeEquivalentTo(new[] { 10, 11 });
    }

    [Fact]
    public async Task GetList_SuperAdminWithoutSupplierCode_SeesSuperAdminAndTemplates()
    {
        _access.SuperAdmins.Add(CallerId);
        _tenant.SupplierCodeId = null;
        _tenant.CompanyId = null;

        var list = await _service.GetListAsync(CancellationToken.None);

        list.Select(r => r.Id).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Theory]
    [InlineData(20)]     // another company's role
    [InlineData(2)]      // a template
    [InlineData(999)]    // unknown
    public async Task GetById_NotInCallersCompany_Returns404(int id)
    {
        await _service.Invoking(s => s.GetByIdAsync(id, CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    // ---------- create / update / delete ----------

    [Fact]
    public async Task Create_InCallersCompany_WithPolicyLink_Audited()
    {
        var response = await _service.CreateAsync(Save(), CancellationToken.None);

        response.CompanyId.Should().Be(Company);
        response.PasswordPolicyId.Should().Be(2);
        _roles.PolicyLinks.Should().ContainSingle(l => l.RoleId == response.Id && l.PasswordPolicyId == 2);
        _log.Has(UserLogActions.RoleCreated).Should().BeTrue();
    }

    [Fact]
    public async Task Create_SameNameInSameCompany_Returns409()
    {
        var ex = await _service.Invoking(s => s.CreateAsync(Save("operator"), CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.RoleNameTaken);
    }

    [Fact]
    public async Task Create_UnknownPasswordPolicy_Returns404()
    {
        await _service.Invoking(s => s.CreateAsync(Save(policy: 99), CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_AdminRole_NeedsManageAdmin()
    {
        var ex = await _service.Invoking(s => s.CreateAsync(Save(admin: true), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
        ex.Which.ErrorCode.Should().Be(ErrorCodes.AdminUserProtected);

        _access.Grant(CallerId, OwnSupplierCode, Permissions.UserManageAdmin);
        var service = new RoleService(_roles, new CurrentAccess(_access, new FakeCurrentUser(CallerId), _tenant), _clock, _log,
            new SaveRoleRequestValidator(), new UpdateRoleRightsRequestValidator());

        (await service.CreateAsync(Save(admin: true), CancellationToken.None)).IsAdminRole.Should().BeTrue();
    }

    [Fact]
    public async Task Create_SuperAdminWithoutSupplierCode_CreatesTemplate()
    {
        _access.SuperAdmins.Add(CallerId);
        _tenant.SupplierCodeId = null;
        _tenant.CompanyId = null;

        var response = await _service.CreateAsync(Save("Quality"), CancellationToken.None);

        response.IsTemplate.Should().BeTrue();
        response.CompanyId.Should().BeNull();
    }

    [Fact]
    public async Task Update_SuperAdminRole_Returns409NotEditable()
    {
        _access.SuperAdmins.Add(CallerId);

        var ex = await _service.Invoking(s => s.UpdateAsync(1, Save("Boss"), CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.RoleNotEditable);
    }

    [Fact]
    public async Task Update_AdminRole_WithoutManageAdmin_Returns403()
    {
        await _service.Invoking(s => s.UpdateAsync(11, Save("Plant Head", admin: true), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Update_RenamesAndChangesPolicy()
    {
        _roles.PolicyLinks.Add(ROLE_PASSWORD_POLICY.Create(10, 2, TestData.Now, null));

        var response = await _service.UpdateAsync(10, Save("Line Operator", policy: 3), CancellationToken.None);

        response.RoleName.Should().Be("Line Operator");
        _roles.PolicyLinks.Single(l => l.RoleId == 10).PasswordPolicyId.Should().Be(3);
    }

    [Fact]
    public async Task Delete_RoleStillAssigned_Returns409()
    {
        _roles.AssignedRoles.Add(10);

        var ex = await _service.Invoking(s => s.DeleteAsync(10, CancellationToken.None)).Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.RoleInUse);
        _roles.Roles.Should().Contain(r => r.Id == 10);
    }

    [Fact]
    public async Task Delete_UnusedRole_RemovesIt()
    {
        var response = await _service.DeleteAsync(10, CancellationToken.None);

        response.Message.Should().Contain("Operator");
        _roles.Roles.Should().NotContain(r => r.Id == 10);
        _log.Has(UserLogActions.RoleDeleted).Should().BeTrue();
    }

    // ---------- rights ----------

    [Fact]
    public async Task UpdateRights_ReplacesTheSet_AndShowsItTicked()
    {
        _roles.Rights[10] = new HashSet<int> { RoleView };

        var grid = await _service.UpdateRightsAsync(10, Rights(RoleView, UserUnlock), CancellationToken.None);

        _roles.Rights[10].Should().BeEquivalentTo(new[] { RoleView, UserUnlock });
        grid.Pages.SelectMany(p => p.Actions).Where(a => a.Granted).Select(a => a.PageActionId)
            .Should().BeEquivalentTo(new[] { RoleView, UserUnlock });
        _log.Has(UserLogActions.RoleRightsChanged).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRights_SystemRight_Returns403NotGrantable()
    {
        var ex = await _service.Invoking(s => s.UpdateRightsAsync(10, Rights(SupplierAdd), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.RightNotGrantable);
    }

    [Fact]
    public async Task UpdateRights_AddingAdminRight_WithoutManageAdmin_Returns403()
    {
        var ex = await _service.Invoking(s => s.UpdateRightsAsync(10, Rights(SecurityEdit), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.AdminUserProtected);
    }

    [Fact]
    public async Task UpdateRights_AdminRightAlreadyThere_UnchangedSaveIsAllowed()
    {
        _roles.Rights[10] = new HashSet<int> { SecurityEdit };

        await _service.UpdateRightsAsync(10, Rights(SecurityEdit, RoleView), CancellationToken.None);

        _roles.Rights[10].Should().BeEquivalentTo(new[] { SecurityEdit, RoleView });
    }

    [Fact]
    public async Task UpdateRights_UnknownAction_Returns400()
    {
        var ex = await _service.Invoking(s => s.UpdateRightsAsync(10, Rights(999), CancellationToken.None))
            .Should().ThrowAsync<ValidationException>();

        ex.Which.Errors.Should().ContainKey("pageActionIds");
    }

    // ---------- templates ----------

    [Fact]
    public async Task Templates_CopiedIntoNewCompany_WithRightsAndPolicy_OnlyOnce()
    {
        _roles.Rights[2] = new HashSet<int> { RoleView };
        _roles.PolicyLinks.Add(ROLE_PASSWORD_POLICY.Create(2, 2, TestData.Now, null));
        var templates = new RoleTemplates(_roles, _clock, new FakeCurrentUser(CallerId), _log);

        var copied = await templates.CopyIntoCompanyAsync(companyId: 5, CancellationToken.None);
        var again = await templates.CopyIntoCompanyAsync(companyId: 5, CancellationToken.None);

        copied.Should().Be(1);
        again.Should().Be(0);
        var copy = _roles.Roles.Single(r => r.CompanyId == 5);
        copy.RoleName.Should().Be("Operator");
        copy.IsTemplate.Should().BeFalse();
        _roles.Rights[copy.Id].Should().BeEquivalentTo(new[] { RoleView });
        _roles.PolicyLinks.Should().Contain(l => l.RoleId == copy.Id && l.PasswordPolicyId == 2);
    }
}
