using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;

namespace ST.LiquorTNT.Api.Middleware;

/// <summary>
/// Runs after JWT authentication. A valid signature is not enough: the session behind the token must
/// still be ACTIVE and inside its deadline in USER_SESSION. That is what makes logout, revocation, the
/// idle window and the hard limit real. The client is told which one ended the session:
/// <list type="bullet">
/// <item>SESSION_TIMED_OUT — no call for SESSION_IDLE_MINUTES: go to the login page.</item>
/// <item>SESSION_EXPIRED — the hard limit SESSION_EXPIRY_MINUTES: password popup, then retry the call.</item>
/// <item>SESSION_INVALID — logged out, revoked or unknown: go to the login page.</item>
/// </list>
/// Activity slides the idle deadline forward; it is written at most once a minute, and only then is the
/// idle window read from SECURITY_CONFIG. It also loads the supplier code picked for the session (one small indexed
/// read when a supplier code is picked) and hands it to TenantContext.
/// <para>
/// A session opened before the user has a security question (first login) may only reach endpoints marked
/// <see cref="AllowWithoutSecurityQuestionAttribute"/>; everything else is 403 SECURITY_QUESTION_REQUIRED.
/// </para>
/// </summary>
public sealed class SessionValidationMiddleware
{
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromMinutes(1);

    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ISessionRepository sessions,
        ITokenHasher tokenHasher,
        IRequestContext request,
        ISecurityConfigProvider config,
        IClock clock,
        IUserLogWriter log,
        IAccessRepository supplierCodes)
    {
        // Anonymous endpoints (login, forgot-password ...) must work even if the client still sends an old token.
        var anonymous = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() is not null;

        if (!anonymous && context.User.Identity?.IsAuthenticated == true)
        {
            var ct = context.RequestAborted;
            var token = request.AccessToken
                        ?? throw new UnauthorizedException(ErrorCodes.SessionInvalid, "No session token was presented.");

            var session = await sessions.GetByTokenHashAsync(tokenHasher.Hash(token), ct);
            var now = clock.IndiaNow;

            if (session is null || session.Status != Domain.Entities.USER_SESSION.StatusActive)
            {
                throw new UnauthorizedException(ErrorCodes.SessionInvalid, "This session has ended.", "Log in again.");
            }

            if (session.ExpiresAt <= now)
            {
                var hardLimit = session.ReachedLimitAt(now);
                session.Expire();
                await log.WriteAsync(UserLogEntry.Success(UserLogActions.SessionExpired, UserLogModules.Auth,
                    "USER_SESSION", session.Id.ToString(),
                    hardLimit ? "Session reached its hard limit." : "Session ended after the idle window.",
                    actorUserId: session.UserId), ct);
                await sessions.SaveChangesAsync(ct);

                throw hardLimit
                    ? new UnauthorizedException(ErrorCodes.SessionExpired, "This session has reached its time limit.",
                        "Enter your password again to continue.")
                    : new UnauthorizedException(ErrorCodes.SessionTimedOut, "This session ended because it was idle.",
                        "Log in again.");
            }

            if (session.LastActivityAt is null || now - session.LastActivityAt.Value >= ActivityWriteInterval)
            {
                var settings = await config.GetAsync(ct);
                session.Slide(now, settings.SessionIdleMinutes);
                await sessions.SaveChangesAsync(ct);
            }

            // First login: the security question comes before any other screen.
            if (session.SecurityQuestionPending
                && context.GetEndpoint()?.Metadata.GetMetadata<AllowWithoutSecurityQuestionAttribute>() is null)
            {
                throw new ForbiddenException(ErrorCodes.SecurityQuestionRequired, "Security question not set.",
                    "Choose a security question and answer (PUT /api/securityquestions/mine) before continuing.");
            }

            // The supplier code picked for this session scopes the whole call (rights and data). A supplier code that was
            // deactivated meanwhile counts as "not picked", so the user is sent back to the picker.
            if (session.ActiveSupplierCodeId is int supplierCodeId
                && await supplierCodes.GetSupplierCodeAsync(supplierCodeId, ct) is { } supplierCode)
            {
                context.Items[SessionScope.ItemKey] = new SessionScope(supplierCode.Id, supplierCode.CompanyId, supplierCode.ExciseCode);
            }
        }

        await _next(context);
    }
}
