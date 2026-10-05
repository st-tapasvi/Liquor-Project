using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AdministratorRequirement.PolicyName)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var created = await _users.CreateAsync(request, ct);
        return Created($"/api/users/{created.Id}", created);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(int id, CancellationToken ct)
        => Ok(await _users.GetByIdAsync(id, ct));

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetPageAsync([FromQuery] UserListRequest request, CancellationToken ct)
        => Ok(await _users.GetPageAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
        => Ok(await _users.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<UserResponse>> ActivateAsync(int id, CancellationToken ct)
        => Ok(await _users.ActivateAsync(id, ct));

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<UserResponse>> DeactivateAsync(int id, CancellationToken ct)
        => Ok(await _users.DeactivateAsync(id, ct));

    [HttpPost("{id:int}/unlock")]
    public async Task<ActionResult<UserResponse>> UnlockAsync(int id, CancellationToken ct)
        => Ok(await _users.UnlockAsync(id, ct));
}
