using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>Forgot password. All three steps are anonymous — the user has no session yet.</summary>
[ApiController]
[Route("api/auth/forgotpassword")]
[AllowAnonymous]
public sealed class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _reset;

    public PasswordResetController(IPasswordResetService reset) => _reset = reset;

    [HttpPost("start")]
    public async Task<ActionResult<ForgotPasswordStartResponse>> StartAsync(ForgotPasswordStartRequest request, CancellationToken ct)
        => Ok(await _reset.StartAsync(request, ct));

    [HttpPost("verify")]
    public async Task<ActionResult<MessageResponse>> VerifyAsync(ForgotPasswordVerifyRequest request, CancellationToken ct)
        => Ok(await _reset.VerifyAsync(request, ct));

    [HttpPost("reset")]
    public async Task<ActionResult<MessageResponse>> ResetAsync(ForgotPasswordResetRequest request, CancellationToken ct)
        => Ok(await _reset.ResetAsync(request, ct));
}
