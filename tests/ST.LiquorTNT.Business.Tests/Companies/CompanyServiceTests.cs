using FluentAssertions;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Companies;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Contracts.Companies;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Companies;

public sealed class CompanyServiceTests
{
    private const int CallerId = 1;

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public int? LastScope { get; private set; } = -1;

        public Task<IReadOnlyList<CompanyResponse>> GetListAsync(int? companyId, CancellationToken ct)
        {
            LastScope = companyId;
            return Task.FromResult<IReadOnlyList<CompanyResponse>>(new List<CompanyResponse>());
        }
    }

    private readonly FakeCompanyRepository _companies = new();
    private readonly FakeAccessRepository _access = new();
    private readonly FakeTenantContext _tenant = new();

    private CompanyService Service() => new(_companies, new CurrentAccess(_access, new FakeCurrentUser(CallerId), _tenant));

    [Fact]
    public async Task SuperAdminWithoutSupplierCode_SeesEveryCompany()
    {
        _access.SuperAdmins.Add(CallerId);

        await Service().GetListAsync(CancellationToken.None);

        _companies.LastScope.Should().BeNull();
    }

    [Fact]
    public async Task CompanyUser_IsLimitedToOwnCompany_EvenIfTheRightWereGiven()
    {
        _tenant.CompanyId = 20;
        _tenant.SupplierCodeId = 30;

        await Service().GetListAsync(CancellationToken.None);

        _companies.LastScope.Should().Be(20);
    }
}
