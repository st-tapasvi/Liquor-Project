using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Contracts.Roles;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>Every page with its actions: the empty rights grid for the role and user-access screens.</summary>
[ApiController]
[Route("api/pages")]
public sealed class PagesController : ControllerBase
{
    private readonly IRoleService _roles;

    public PagesController(IRoleService roles) => _roles = roles;

    [HttpGet]
    [HasPermission(Permissions.RoleView)]
    public async Task<ActionResult<IReadOnlyList<PageResponse>>> GetAsync(CancellationToken ct)
        => Ok(await _roles.GetPagesAsync(ct));
}
