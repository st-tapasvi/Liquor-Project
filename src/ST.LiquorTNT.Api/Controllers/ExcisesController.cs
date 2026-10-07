using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Excises;
using ST.LiquorTNT.Contracts.Excises;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>State excise departments, for dropdowns (part of the supplier code master, so suppliercode.view).</summary>
[ApiController]
[Route("api/excises")]
public sealed class ExcisesController : ControllerBase
{
    private readonly IExciseService _excises;

    public ExcisesController(IExciseService excises) => _excises = excises;

    [HttpGet]
    [HasPermission(Permissions.SupplierCodeView)]
    public async Task<ActionResult<IReadOnlyList<ExciseResponse>>> GetAllAsync(CancellationToken ct)
        => Ok(await _excises.GetAllAsync(ct));
}
