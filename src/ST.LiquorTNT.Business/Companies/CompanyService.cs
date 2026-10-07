using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Contracts.Companies;

namespace ST.LiquorTNT.Business.Companies;

/// <summary>
/// The company list for Super Admin's screens (supplier code form, first user of a company). The right behind it,
/// company.view, is SYSTEM scope, so in practice only Super Admin calls it; the company scope is applied anyway, so a
/// company user could never see another company even if the right were ever given.
/// </summary>
public sealed class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companies;
    private readonly CurrentAccess _access;

    public CompanyService(ICompanyRepository companies, CurrentAccess access)
    {
        _companies = companies;
        _access = access;
    }

    public async Task<IReadOnlyList<CompanyResponse>> GetListAsync(CancellationToken ct) =>
        await _companies.GetListAsync(await _access.CompanyScopeAsync(ct), ct);
}
