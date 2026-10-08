using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Roles;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>Master roles of the caller's company and the rights inside each role.</summary>
[ApiController]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles) => _roles = roles;

    /// <summary>
    /// The company's roles: company-level ones first, then per supplier code ("Operator RJ CL 772").
    /// <c>?supplierCodeId=30</c> → only the roles usable in that supplier code (its own + company-level).
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.RoleView)]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> GetListAsync([FromQuery] int? supplierCodeId, CancellationToken ct)
        => Ok(await _roles.GetListAsync(supplierCodeId, ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.RoleView)]
    public async Task<ActionResult<RoleResponse>> GetByIdAsync(int id, CancellationToken ct)
        => Ok(await _roles.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.RoleAdd)]
    public async Task<ActionResult<RoleResponse>> CreateAsync(SaveRoleRequest request, CancellationToken ct)
    {
        var created = await _roles.CreateAsync(request, ct);
        return Created($"/api/roles/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.RoleEdit)]
    public async Task<ActionResult<RoleResponse>> UpdateAsync(int id, SaveRoleRequest request, CancellationToken ct)
        => Ok(await _roles.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.RoleDelete)]
    public async Task<ActionResult<MessageResponse>> DeleteAsync(int id, CancellationToken ct)
        => Ok(await _roles.DeleteAsync(id, ct));

    /// <summary>The rights grid of a role: every page and action, with "granted" ticked.</summary>
    [HttpGet("{id:int}/rights")]
    [HasPermission(Permissions.RoleView)]
    public async Task<ActionResult<RoleRightsResponse>> GetRightsAsync(int id, CancellationToken ct)
        => Ok(await _roles.GetRightsAsync(id, ct));

    /// <summary>Send the FULL list of ticked page actions; anything not in it is removed from the role.</summary>
    [HttpPut("{id:int}/rights")]
    [HasPermission(Permissions.RoleEdit)]
    public async Task<ActionResult<RoleRightsResponse>> UpdateRightsAsync(int id, UpdateRoleRightsRequest request, CancellationToken ct)
        => Ok(await _roles.UpdateRightsAsync(id, request, ct));
}
