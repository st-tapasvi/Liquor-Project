using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Contracts.Access;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAccessService _access;
    private readonly ISessionService _sessions;
    private readonly IClock _clock;
    private readonly AuthCookieOptions _cookies;

    public AuthController(IAuthService auth, IAccessService access, ISessionService sessions, IClock clock, IOptions<AuthCookieOptions> cookies)
    {
        _auth = auth;
        _access = access;
        _sessions = sessions;
        _clock = clock;
        _cookies = cookies.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var response = await _auth.LoginAsync(request, ct);
        AuthCookies.Issue(HttpContext, response.AccessToken, response.ExpiresAt - _clock.IndiaNow, _cookies);

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<MessageResponse>> LogoutAsync(CancellationToken ct)
    {
        var response = await _auth.LogoutAsync(ct);
        AuthCookies.Clear(HttpContext, _cookies);

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> MeAsync(CancellationToken ct)
        => Ok(await _auth.GetCurrentUserAsync(ct));

    /// <summary>Anonymous on purpose: it is verified by the current password, and a forced change happens before login.</summary>
    [HttpPost("changepassword")]
    [AllowAnonymous]
    public async Task<ActionResult<MessageResponse>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
        => Ok(await _auth.ChangePasswordAsync(request, ct));

    // ---- supplier code of the session: picked after login, switchable ----

    [HttpGet("mysuppliercodes")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SupplierCodeResponse>>> GetMySupplierCodesAsync(CancellationToken ct)
        => Ok(await _access.GetMySupplierCodesAsync(ct));

    /// <summary>Picks or switches the supplier code. The server checks the user holds it; rights apply from the next call.</summary>
    [HttpPost("selectsuppliercode")]
    [Authorize]
    public async Task<ActionResult<MyPermissionsResponse>> SelectSupplierCodeAsync(SelectSupplierCodeRequest request, CancellationToken ct)
        => Ok(await _access.SelectSupplierCodeAsync(request, ct));

    /// <summary>Permission keys in the selected supplier code, for the menu and buttons (the server still checks every call).</summary>
    [HttpGet("mypermissions")]
    [Authorize]
    public async Task<ActionResult<MyPermissionsResponse>> GetMyPermissionsAsync(CancellationToken ct)
        => Ok(await _access.GetMyPermissionsAsync(ct));

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> GetSessionsAsync(CancellationToken ct)
        => Ok(await _sessions.GetMySessionsAsync(ct));

    [HttpDelete("sessions/{id:int}")]
    [Authorize]
    public async Task<ActionResult<MessageResponse>> RevokeSessionAsync(int id, CancellationToken ct)
        => Ok(await _sessions.RevokeMySessionAsync(id, ct));
}
