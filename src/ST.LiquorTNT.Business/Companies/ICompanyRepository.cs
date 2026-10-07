using ST.LiquorTNT.Contracts.Companies;

namespace ST.LiquorTNT.Business.Companies;

/// <summary>COMPANY data access for lists. Implemented in Infrastructure.</summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Companies by name, with their supplier code count. <paramref name="companyId"/> limits the list to one company;
    /// null = every company (Super Admin).
    /// </summary>
    Task<IReadOnlyList<CompanyResponse>> GetListAsync(int? companyId, CancellationToken ct);
}
