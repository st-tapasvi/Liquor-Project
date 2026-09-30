using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.SecurityConfig;
using ST.LiquorTNT.Contracts.SecurityConfig;

namespace ST.LiquorTNT.Api.Controllers;

[ApiController]
[Route("api/securityconfig")]
[Authorize(Policy = AdministratorRequirement.PolicyName)]
public sealed class SecurityConfigController : ControllerBase
{
    private readonly ISecurityConfigService _config;

    public SecurityConfigController(ISecurityConfigService config) => _config = config;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SecurityConfigResponse>>> GetAsync(CancellationToken ct)
        => Ok(await _config.GetAllAsync(ct));

    [HttpPut("{key}")]
    public async Task<ActionResult<SecurityConfigResponse>> UpdateAsync(string key, UpdateSecurityConfigRequest request, CancellationToken ct)
        => Ok(await _config.UpdateAsync(key, request, ct));
}
