using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.RoleGroups;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.RoleGroups;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>
/// Role groups of the caller's company: named bundles of master roles given to users as one piece. Managed with the
/// role rights; giving a group to a user is <c>PUT /api/users/{id}/rolegroups</c> (user.access).
/// </summary>
[ApiController]
[Route("api/rolegroups")]
public sealed class RoleGroupsController : ControllerBase
{
    private readonly IRoleGroupService _groups;

    public RoleGroupsController(IRoleGroupService groups) => _groups = groups;

    [HttpGet]
    [HasPermission(Permissions.RoleView)]
    public async Task<ActionResult<IReadOnlyList<RoleGroupResponse>>> GetListAsync(CancellationToken ct)
        => Ok(await _groups.GetListAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.RoleView)]
    public async Task<ActionResult<RoleGroupResponse>> GetByIdAsync(int id, CancellationToken ct)
        => Ok(await _groups.GetByIdAsync(id, ct));

    /// <summary>Creates an empty group; its roles are set with <c>PUT /api/rolegroups/{id}/roles</c>.</summary>
    [HttpPost]
    [HasPermission(Permissions.RoleAdd)]
    public async Task<ActionResult<RoleGroupResponse>> CreateAsync(SaveRoleGroupRequest request, CancellationToken ct)
    {
        var created = await _groups.CreateAsync(request, ct);
        return Created($"/api/rolegroups/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.RoleEdit)]
    public async Task<ActionResult<RoleGroupResponse>> UpdateAsync(int id, SaveRoleGroupRequest request, CancellationToken ct)
        => Ok(await _groups.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.RoleDelete)]
    public async Task<ActionResult<MessageResponse>> DeleteAsync(int id, CancellationToken ct)
        => Ok(await _groups.DeleteAsync(id, ct));

    /// <summary>The FULL list of roles of the group; applies to every user of the group on their next call.</summary>
    [HttpPut("{id:int}/roles")]
    [HasPermission(Permissions.RoleEdit)]
    public async Task<ActionResult<RoleGroupResponse>> UpdateRolesAsync(int id, UpdateRoleGroupRolesRequest request, CancellationToken ct)
        => Ok(await _groups.UpdateRolesAsync(id, request, ct));
}
