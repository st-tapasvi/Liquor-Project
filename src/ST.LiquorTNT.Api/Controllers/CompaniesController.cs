using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Companies;
using ST.LiquorTNT.Contracts.Companies;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>Company list for Super Admin's screens (company.view is SYSTEM scope).</summary>
[ApiController]
[Route("api/companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companies;

    public CompaniesController(ICompanyService companies) => _companies = companies;

    [HttpGet]
    [HasPermission(Permissions.CompanyView)]
    public async Task<ActionResult<IReadOnlyList<CompanyResponse>>> GetListAsync(CancellationToken ct)
        => Ok(await _companies.GetListAsync(ct));
}
