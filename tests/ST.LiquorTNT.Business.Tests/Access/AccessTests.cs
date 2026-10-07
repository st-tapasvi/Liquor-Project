using FluentAssertions;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Contracts.Access;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Access;

public sealed class AccessTests
{
    private const int UserId = 10;
    private const int OwnSupplierCode = 100;

    private readonly FakeAccessRepository _access = new();
    private readonly FakeTenantContext _tenant = new() { CompanyId = 1, SupplierCodeId = OwnSupplierCode, ExciseCode = "RJ" };

    private CurrentAccess Current() => new(_access, new FakeCurrentUser(UserId), _tenant);

    // ---------- CurrentAccess: the [HasPermission] check ----------

    [Fact]
    public async Task EnsurePermission_KeyHeldInTheSupplierCode_Passes()
    {
        _access.Grant(UserId, OwnSupplierCode, Permissions.RoleView);

        await Current().EnsurePermissionAsync(Permissions.RoleView, CancellationToken.None);
    }

    [Fact]
    public async Task EnsurePermission_KeyHeldOnlyInAnotherSupplierCode_Returns403()
    {
        _access.Grant(UserId, 200, Permissions.UserUnlock);

        var ex = await Current().Invoking(c => c.EnsurePermissionAsync(Permissions.UserUnlock, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.PermissionDenied);
        ex.Which.Detail.Should().Contain("user.unlock");
    }

    [Fact]
    public async Task EnsurePermission_NoSupplierCodePicked_Returns409SupplierCodeNotSelected()
    {
        _tenant.SupplierCodeId = null;

        var ex = await Current().Invoking(c => c.EnsurePermissionAsync(Permissions.RoleView, CancellationToken.None))
            .Should().ThrowAsync<BusinessException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.SupplierCodeNotSelected);
    }

    [Fact]
    public async Task EnsurePermission_SuperAdmin_AlwaysPasses_EvenWithoutSupplierCode()
    {
        _access.SuperAdmins.Add(UserId);
        _tenant.SupplierCodeId = null;

        await Current().EnsurePermissionAsync(Permissions.SupplierCodeAdd, CancellationToken.None);
    }

    [Fact]
    public async Task CompanyScope_CompanyUserWithoutCompany_Returns403()
    {
        _tenant.CompanyId = null;

        await Current().Invoking(c => c.CompanyScopeAsync(CancellationToken.None)).Should().ThrowAsync<ForbiddenException>();
    }

    // ---------- AccessService: picking a supplier code ----------

    private readonly FakeSessionRepository _sessions = new();
    private readonly FakeRequestContext _request = new() { AccessToken = "tok" };
    private readonly FakeUserLogWriter _log = new();

    private AccessService Service()
    {
        var current = Current();
        return new AccessService(new SupplierCodeDirectory(_access), _access, current, _tenant, _sessions, new FakeTokenHasher(),
            _request, new FixedClock(TestData.Now), _log, new SelectSupplierCodeRequestValidator());
    }

    private async Task<USER_SESSION> OpenSessionAsync()
    {
        var session = USER_SESSION.Create(UserId, "TH:tok", TestData.Now, 60, TestData.Now.AddHours(8), null, null);
        await _sessions.AddAsync(session, CancellationToken.None);
        return session;
    }

    [Fact]
    public async Task SelectSupplierCode_OwnSupplierCode_StoresItInTheSession_ReturnsItsPermissions()
    {
        var session = await OpenSessionAsync();
        _access.SupplierCodesByUser[UserId] = new List<SupplierCodeResponse> { FakeAccessRepository.SupplierCode(200, code: "620") };
        _access.Grant(UserId, 200, Permissions.RoleView, Permissions.UserAdd);

        var response = await Service().SelectSupplierCodeAsync(new SelectSupplierCodeRequest { SupplierCodeId = 200 }, CancellationToken.None);

        session.ActiveSupplierCodeId.Should().Be(200);
        response.ActiveSupplierCode!.Id.Should().Be(200);
        response.Permissions.Should().BeEquivalentTo(Permissions.RoleView, Permissions.UserAdd);
        _log.Has(UserLogActions.SupplierCodeSelected).Should().BeTrue();
    }

    [Fact]
    public async Task SelectSupplierCode_NotTheUsersSupplierCode_Returns403_SessionUnchanged()
    {
        var session = await OpenSessionAsync();

        var ex = await Service().Invoking(s => s.SelectSupplierCodeAsync(new SelectSupplierCodeRequest { SupplierCodeId = 999 }, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();

        ex.Which.ErrorCode.Should().Be(ErrorCodes.SupplierCodeNotAssigned);
        session.ActiveSupplierCodeId.Should().BeNull();
    }

    [Fact]
    public async Task SelectSupplierCode_SuperAdmin_MayPickAnyActiveSupplierCode()
    {
        await OpenSessionAsync();
        _access.SuperAdmins.Add(UserId);
        _access.AllSupplierCodes.Add(FakeAccessRepository.SupplierCode(300, companyId: 7));

        var response = await Service().SelectSupplierCodeAsync(new SelectSupplierCodeRequest { SupplierCodeId = 300 }, CancellationToken.None);

        response.IsSuperAdmin.Should().BeTrue();
        response.ActiveSupplierCode!.CompanyId.Should().Be(7);
    }
}
