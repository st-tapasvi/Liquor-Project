using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>
/// The one way a password (or a security answer) is checked: login, change-password,
/// set-security-question and forgot-password all go through here.
/// Unknown user and wrong password give the same answer; inactive / blocked / locked accounts are
/// refused even with the right password; each wrong attempt is counted on USERS and may lock the account.
/// </summary>
public sealed class CredentialVerifier
{
    private const string EntityName = "USERS";

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUserLogWriter _log;

    public CredentialVerifier(IUserRepository users, IPasswordHasher hasher, IUserLogWriter log)
    {
        _users = users;
        _hasher = hasher;
        _log = log;
    }

    public async Task<USERS> VerifyAsync(string userName, string password, SecuritySettings settings, DateTime now, CancellationToken ct)
    {
        var user = await _users.GetByUserNameAsync(userName.Trim(), ct);

        if (user is null)
        {
            await _log.WriteAsync(UserLogEntry.Failed(UserLogActions.LoginFailed, UserLogModules.Auth,
                "Unknown user name.", EntityName, null, actorUserId: null), ct);
            await _users.SaveChangesAsync(ct);
            throw new UnauthorizedException(ErrorCodes.InvalidCredentials, "User name or password is incorrect.");
        }

        await EnsureMayAuthenticateAsync(user, now, ct);

        if (!_hasher.Verify(password, user.PasswordHash ?? string.Empty))
        {
            await RegisterWrongAttemptAsync(user, settings, now, "Wrong password", ct);

            // The account was unlocked a moment ago (checked above), so a lock now means this attempt set it.
            throw user.IsLockedAt(now)
                ? JustLockedException(user)
                : new UnauthorizedException(ErrorCodes.InvalidCredentials, "User name or password is incorrect.");
        }

        return user;
    }

    /// <summary>Refuses an account that is inactive, blocked or temporarily locked, auditing the refusal.</summary>
    public async Task EnsureMayAuthenticateAsync(USERS user, DateTime now, CancellationToken ct)
    {
        if (!user.IsActive)
        {
            await FailAsync(user, "Account is inactive.", ct);
            throw new ForbiddenException(ErrorCodes.UserInactive, "This user is inactive.");
        }

        if (user.IsBlocked)
        {
            await FailAsync(user, "Account is blocked.", ct);
            throw new ForbiddenException(ErrorCodes.UserBlocked, "This user is blocked.", "An administrator has to unblock the account.");
        }  

        if (user.IsLockedAt(now))
        {
            await FailAsync(user, "Account is temporarily locked.", ct);
            throw AlreadyLockedException(user);
        }
    }

    /// <summary>A wrong password or wrong security answer: counted towards the lock, audited and committed.</summary>
    public async Task RegisterWrongAttemptAsync(USERS user, SecuritySettings settings, DateTime now, string what, CancellationToken ct)
    {
        var lockedNow = user.RegisterFailedLogin(now, settings.FailedLoginLockEnabled,
            settings.MaxFailedLoginAttempts, settings.AccountLockDurationMinutes);

        await _log.WriteAsync(UserLogEntry.Failed(UserLogActions.LoginFailed, UserLogModules.Auth,
            $"{what} (attempt {user.FailedLoginAttempts}).", EntityName, user.Id.ToString(), actorUserId: user.Id), ct);

        if (lockedNow)
        {
            await _log.WriteAsync(UserLogEntry.Success(UserLogActions.AccountLocked, UserLogModules.Auth,
                EntityName, user.Id.ToString(),
                $"Locked until {user.LockedUntil:yyyy-MM-dd HH:mm} after {user.FailedLoginAttempts} failed attempts.",
                actorUserId: user.Id), ct);
        }

        await _users.SaveChangesAsync(ct);
    }

    /// <summary>This wrong attempt reached the limit and locked the account.</summary>
    public static ForbiddenException JustLockedException(USERS user) =>
        new(ErrorCodes.UserLocked, $"Account locked after {user.FailedLoginAttempts} failed attempts.",
            $"Try again after {user.LockedUntil:yyyy-MM-dd HH:mm} or ask an administrator to unlock.");

    /// <summary>The account was locked before this attempt; the attempt was not checked or counted.</summary>
    public static ForbiddenException AlreadyLockedException(USERS user) =>
        new(ErrorCodes.UserLocked, "This account is already locked.",
            $"Locked until {user.LockedUntil:yyyy-MM-dd HH:mm}. Ask an administrator to unlock it, or try again after that time.");

    private async Task FailAsync(USERS user, string reason, CancellationToken ct)
    {
        await _log.WriteAsync(UserLogEntry.Failed(UserLogActions.LoginFailed, UserLogModules.Auth, reason,
            EntityName, user.Id.ToString(), actorUserId: user.Id), ct);
        await _users.SaveChangesAsync(ct);
    }
}
