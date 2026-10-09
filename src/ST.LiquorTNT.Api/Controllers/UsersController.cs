using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IUserAccessService _access;

    public UsersController(IUserService users, IUserAccessService access)
    {
        _users = users;
        _access = access;
    }

    [HttpPost]
    [HasPermission(Permissions.UserAdd)]
    public async Task<ActionResult<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var created = await _users.CreateAsync(request, ct);
        return Created($"/api/users/{created.Id}", created);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.UserView)]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(int id, CancellationToken ct)
        => Ok(await _users.GetByIdAsync(id, ct));

    [HttpGet]
    [HasPermission(Permissions.UserView)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetPageAsync([FromQuery] UserListRequest request, CancellationToken ct)
        => Ok(await _users.GetPageAsync(request, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.UserEdit)]
    public async Task<ActionResult<UserResponse>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
        => Ok(await _users.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/activate")]
    [HasPermission(Permissions.UserStatus)]
    public async Task<ActionResult<UserResponse>> ActivateAsync(int id, CancellationToken ct)
        => Ok(await _users.ActivateAsync(id, ct));

    [HttpPost("{id:int}/deactivate")]
    [HasPermission(Permissions.UserStatus)]
    public async Task<ActionResult<UserResponse>> DeactivateAsync(int id, CancellationToken ct)
        => Ok(await _users.DeactivateAsync(id, ct));

    [HttpPost("{id:int}/unlock")]
    [HasPermission(Permissions.UserUnlock)]
    public async Task<ActionResult<UserResponse>> UnlockAsync(int id, CancellationToken ct)
        => Ok(await _users.UnlockAsync(id, ct));

    // ---- roles (per supplier code) and custom rights of a user ----

    [HttpGet("{id:int}/access")]
    [HasPermission(Permissions.UserView)]
    public async Task<ActionResult<UserAccessResponse>> GetAccessAsync(int id, CancellationToken ct)
        => Ok(await _access.GetAsync(id, ct));

    /// <summary>The FULL list of the user's roles; anything not in it is taken away.</summary>
    [HttpPut("{id:int}/roles")]
    [HasPermission(Permissions.UserAccess)]
    public async Task<ActionResult<UserAccessResponse>> UpdateRolesAsync(int id, UpdateUserRolesRequest request, CancellationToken ct)
        => Ok(await _access.UpdateRolesAsync(id, request, ct));

    /// <summary>The FULL list of the user's role groups; anything not in it is taken away.</summary>
    [HttpPut("{id:int}/rolegroups")]
    [HasPermission(Permissions.UserAccess)]
    public async Task<ActionResult<UserAccessResponse>> UpdateRoleGroupsAsync(int id, UpdateUserRoleGroupsRequest request, CancellationToken ct)
        => Ok(await _access.UpdateRoleGroupsAsync(id, request, ct));

    /// <summary>The FULL list of the user's custom rights; anything not in it is taken away.</summary>
    [HttpPut("{id:int}/rights")]
    [HasPermission(Permissions.UserAccess)]
    public async Task<ActionResult<UserAccessResponse>> UpdateRightsAsync(int id, UpdateUserRightsRequest request, CancellationToken ct)
        => Ok(await _access.UpdateRightsAsync(id, request, ct));
}
