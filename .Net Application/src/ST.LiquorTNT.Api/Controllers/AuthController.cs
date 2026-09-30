using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;

namespace ST.LiquorTNT.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ISessionService _sessions;

    public AuthController(IAuthService auth, ISessionService sessions)
    {
        _auth = auth;
        _sessions = sessions;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
        => Ok(await _auth.LoginAsync(request, ct));

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<MessageResponse>> LogoutAsync(CancellationToken ct)
        => Ok(await _auth.LogoutAsync(ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> MeAsync(CancellationToken ct)
        => Ok(await _auth.GetCurrentUserAsync(ct));

    /// <summary>Anonymous on purpose: it is verified by the current password, and a forced change happens before login.</summary>
    [HttpPost("changepassword")]
    [AllowAnonymous]
    public async Task<ActionResult<MessageResponse>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
        => Ok(await _auth.ChangePasswordAsync(request, ct));

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> GetSessionsAsync(CancellationToken ct)
        => Ok(await _sessions.GetMySessionsAsync(ct));

    [HttpDelete("sessions/{id:int}")]
    [Authorize]
    public async Task<ActionResult<MessageResponse>> RevokeSessionAsync(int id, CancellationToken ct)
        => Ok(await _sessions.RevokeMySessionAsync(id, ct));
}
