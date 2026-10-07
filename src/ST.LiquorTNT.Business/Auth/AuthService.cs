using FluentValidation;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>
/// Login, logout and password change. Audit rows are staged before saving, so the state change and
/// its audit trail are committed together. All repositories share the request's DbContext, so one
/// SaveChanges commits everything; the extra calls are harmless and keep each fake honest in tests.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const string EntityName = "USERS";

    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly CredentialVerifier _credentials;
    private readonly IPasswordHasher _hasher;
    private readonly IAccessTokenService _tokens;
    private readonly ITokenHasher _tokenHasher;
    private readonly ISecurityConfigProvider _config;
    private readonly PasswordRules _passwordRules;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _request;
    private readonly IUserLogWriter _log;
    private readonly SupplierCodeDirectory _supplierCodes;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public AuthService(
        IUserRepository users,
        ISessionRepository sessions,
        CredentialVerifier credentials,
        IPasswordHasher hasher,
        IAccessTokenService tokens,
        ITokenHasher tokenHasher,
        ISecurityConfigProvider config,
        PasswordRules passwordRules,
        IClock clock,
        ICurrentUser currentUser,
        IRequestContext request,
        IUserLogWriter log,
        SupplierCodeDirectory supplierCodes,
        IValidator<LoginRequest> loginValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _users = users;
        _sessions = sessions;
        _credentials = credentials;
        _hasher = hasher;
        _tokens = tokens;
        _tokenHasher = tokenHasher;
        _config = config;
        _passwordRules = passwordRules;
        _clock = clock;
        _currentUser = currentUser;
        _request = request;
        _log = log;
        _supplierCodes = supplierCodes;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        (await _loginValidator.ValidateAsync(request, ct)).EnsureValid();

        var settings = await _config.GetAsync(ct);
        var now = _clock.IndiaNow;
        var user = await _credentials.VerifyAsync(request.UserName, request.Password, settings, now, ct);

        // The password is right; now the account must be in a state that may open a session.
        if (user.ForcePasswordChange)
        {
            throw new ForbiddenException(ErrorCodes.PasswordChangeRequired,
                "Password change required.", "Set a new password before logging in.");
        }

        if (user.IsPasswordExpiredAt(now))
        {
            throw new ForbiddenException(ErrorCodes.PasswordExpired,
                "Password has expired.", "Set a new password before logging in.");
        }

        if (settings.SessionLimitEnabled
            && await _sessions.CountActiveAsync(user.Id, now, ct) >= settings.MaxActiveSessions)
        {
            await _log.WriteAsync(UserLogEntry.Failed(UserLogActions.LoginFailed, UserLogModules.Auth,
                "Maximum active sessions reached.", EntityName, user.Id.ToString(), actorUserId: user.Id), ct);
            await _users.SaveChangesAsync(ct);

            throw new BusinessException(ErrorCodes.SessionLimitReached,
                "Maximum active sessions reached.",
                $"This account already has {settings.MaxActiveSessions} active session(s). Log out of another device first.");
        }

        // Hard limit: the JWT and the session end together. Inside it, the session slides with activity.
        var expiresAt = now.AddMinutes(settings.SessionExpiryMinutes);
        var token = _tokens.Create(user, _clock.UtcNow.AddMinutes(settings.SessionExpiryMinutes));
        var session = USER_SESSION.Create(user.Id, _tokenHasher.Hash(token), now, settings.SessionIdleMinutes, expiresAt,
            _request.IpAddress, _request.UserAgent);

        // The supplier codes this user may work in. Exactly one → picked straight away, so the user can start working;
        // more than one → the screen shows a picker (POST /api/auth/selectsuppliercode).
        var supplierCodes = await _supplierCodes.ForUserAsync(user.Id, ct);
        var activeSupplierCode = supplierCodes.Count == 1 ? supplierCodes[0] : null;
        if (activeSupplierCode is not null)
        {
            session.SelectSupplierCode(activeSupplierCode.Id);
        }

        await _sessions.AddAsync(session, ct);
        user.RegisterSuccessfulLogin(now, _request.IpAddress);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.LoginSuccess, UserLogModules.Auth,
            EntityName, user.Id.ToString(), $"User '{user.UserName}' logged in.", actorUserId: user.Id), ct);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.SessionCreated, UserLogModules.Auth,
            "USER_SESSION", null,
            $"Session created: idle limit {settings.SessionIdleMinutes} min, hard limit {expiresAt:yyyy-MM-dd HH:mm}.", actorUserId: user.Id), ct);

        await _sessions.SaveChangesAsync(ct);
        await _users.SaveChangesAsync(ct);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            IdleTimeoutMinutes = settings.SessionIdleMinutes,
            User = ToCurrentUser(user),
            SupplierCodes = supplierCodes,
            ActiveSupplierCode = activeSupplierCode,
        };
    }

    public async Task<MessageResponse> LogoutAsync(CancellationToken ct)
    {
        var token = _request.AccessToken
                    ?? throw new UnauthorizedException(ErrorCodes.SessionInvalid, "No session token was presented.");

        var now = _clock.IndiaNow;
        var session = await _sessions.GetByTokenHashAsync(_tokenHasher.Hash(token), ct);
        if (session is null || !session.IsActiveAt(now))
        {
            // ended between the middleware check and here — nothing left to do
            return MessageResponse.Of("This session had already ended.");
        }

        session.Logout(now);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.Logout, UserLogModules.Auth,
            "USER_SESSION", session.Id.ToString(), "User logged out.", actorUserId: session.UserId), ct);
        await _sessions.SaveChangesAsync(ct);

        return MessageResponse.Of("Logged out.");
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId
                     ?? throw new UnauthorizedException(ErrorCodes.SessionInvalid, "Not authenticated.");

        var user = await _users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User");
        return ToCurrentUser(user);
    }

    public async Task<MessageResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        (await _changePasswordValidator.ValidateAsync(request, ct)).EnsureValid();

        var settings = await _config.GetAsync(ct);
        var now = _clock.IndiaNow;
        var user = await _credentials.VerifyAsync(request.UserName, request.CurrentPassword, settings, now, ct);

        var policy = await _passwordRules.EnsureAcceptableForUserAsync(user, request.NewPassword, ct);

        user.SetPassword(_hasher.Hash(request.NewPassword), policy.ExpiryFrom(now), forceChange: false, now, user.Id);

        // A changed password ends every open session; the user signs in again with the new one.
        foreach (var session in await _sessions.GetActiveForUserAsync(user.Id, now, ct))
        {
            session.Revoke(now);
        }

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.PasswordChanged, UserLogModules.Auth,
            EntityName, user.Id.ToString(), $"User '{user.UserName}' changed password.", actorUserId: user.Id), ct);
        await _sessions.SaveChangesAsync(ct);
        await _users.SaveChangesAsync(ct);

        return MessageResponse.Of("Password changed. All sessions have been ended; log in with the new password.");
    }

    private static CurrentUserResponse ToCurrentUser(USERS user) => new()
    {
        UserId = user.Id,
        UserName = user.UserName,
        FullName = user.FullName,
        CompanyId = user.CompanyId,
        ForcePasswordChange = user.ForcePasswordChange,
        PasswordExpiresAt = user.PasswordExpiresAt,
    };
}
