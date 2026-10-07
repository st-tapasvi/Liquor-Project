using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.PasswordPolicies;
using ST.LiquorTNT.Contracts.PasswordPolicies;

namespace ST.LiquorTNT.Api.Controllers;

[ApiController]
[Route("api/passwordpolicies")]
public sealed class PasswordPoliciesController : ControllerBase
{
    private readonly IPasswordPolicyService _policies;

    public PasswordPoliciesController(IPasswordPolicyService policies) => _policies = policies;

    [HttpGet]
    [HasPermission(Permissions.PasswordPolicyView)]
    public async Task<ActionResult<IReadOnlyList<PasswordPolicyResponse>>> GetAsync(CancellationToken ct)
        => Ok(await _policies.GetAllAsync(ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.PasswordPolicyEdit)]
    public async Task<ActionResult<PasswordPolicyResponse>> UpdateAsync(int id, UpdatePasswordPolicyRequest request, CancellationToken ct)
        => Ok(await _policies.UpdateAsync(id, request, ct));
}
