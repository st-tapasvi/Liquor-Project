using ST.LiquorTNT.Contracts.Companies;

namespace ST.LiquorTNT.Business.Companies;

public interface ICompanyService
{
    Task<IReadOnlyList<CompanyResponse>> GetListAsync(CancellationToken ct);
}
