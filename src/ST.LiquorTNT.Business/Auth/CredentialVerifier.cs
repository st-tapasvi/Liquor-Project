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

    private const string Layer = "Business";

    public async Task<USERS> VerifyAsync(string userName, string password, SecuritySettings settings, DateTime now, CancellationToken ct)
    {
        using var step = CallTrace.Step(Layer, "CredentialVerifier.VerifyAsync", new { userName, password, settings, now });

        USERS? user;
        using (var lookup = CallTrace.Step("Infrastructure", "UserRepository.GetByUserNameAsync", new { userName = userName.Trim() }))
        {
            user = await _users.GetByUserNameAsync(userName.Trim(), ct);      // the interceptor nests the SQL under this
            lookup?.Output(user is null ? "no matching user" : $"found user id {user.Id}");
        }

        if (user is null)
        {
            await _log.WriteAsync(UserLogEntry.Failed(UserLogActions.LoginFailed, UserLogModules.Auth,
                "Unknown user name.", EntityName, null, actorUserId: null), ct);
            await _users.SaveChangesAsync(ct);
            CallTrace.Fail(Layer, "CredentialVerifier.VerifyAsync", ErrorCodes.InvalidCredentials, "Unknown user name");
            step?.Output("throw INVALID_CREDENTIALS");
            throw new UnauthorizedException(ErrorCodes.InvalidCredentials, "User name or password is incorrect.");
        }

        await EnsureMayAuthenticateAsync(user, now, ct);

        var matched = _hasher.Verify(password, user.PasswordHash ?? string.Empty);
        CallTrace.Note("Infrastructure", "Pbkdf2PasswordHasher.Verify",
            input: new { password, storedHash = user.PasswordHash }, output: Credential(password, matched));

        if (!matched)
        {
            await RegisterWrongAttemptAsync(user, settings, now, "Wrong password", ct);

            // The account was unlocked a moment ago (checked above), so a lock now means this attempt set it.
            var locked = user.IsLockedAt(now);
            CallTrace.Fail(Layer, "CredentialVerifier.VerifyAsync", ErrorCodes.InvalidCredentials,
                locked ? "Wrong password — account now locked" : $"Wrong password (attempt {user.FailedLoginAttempts})");
            step?.Output(locked ? "throw USER_LOCKED" : "throw INVALID_CREDENTIALS");

            throw locked
                ? JustLockedException(user)
                : new UnauthorizedException(ErrorCodes.InvalidCredentials, "User name or password is incorrect.");
        }

        step?.Output($"password matched, user id {user.Id}");
        return user;
    }

    /// <summary>What the typed password looked like — enough to spot a typo, a stray space or caps lock — without its value.</summary>
    private static object Credential(string password, bool matched) => new
    {
        matched,
        length = password.Length,
        hasLeadingOrTrailingSpace = password.Length != password.Trim().Length,
        hasUppercase = password.Any(char.IsUpper),
        hasLowercase = password.Any(char.IsLower),
        hasDigit = password.Any(char.IsDigit),
        hasSpecial = password.Any(c => !char.IsLetterOrDigit(c)),
    };

    /// <summary>Refuses an account that is inactive, blocked or temporarily locked, auditing the refusal.</summary>
    public async Task EnsureMayAuthenticateAsync(USERS user, DateTime now, CancellationToken ct)
    {
        // A step (not a leaf) so the audit SQL written on a refusal nests inside it.
        using var step = CallTrace.Step(Layer, "CredentialVerifier.EnsureMayAuthenticate", new { user = Summary(user), now });
        step?.Output(new { active = user.IsActive, blocked = user.IsBlocked, locked = user.IsLockedAt(now) });

        if (!user.IsActive)
        {
            await FailAsync(user, "Account is inactive.", ct);
            CallTrace.Fail(Layer, "CredentialVerifier.EnsureMayAuthenticate", ErrorCodes.UserInactive, "Account is inactive");
            throw new ForbiddenException(ErrorCodes.UserInactive, "This user is inactive.");
        }

        if (user.IsBlocked)
        {
            await FailAsync(user, "Account is blocked.", ct);
            CallTrace.Fail(Layer, "CredentialVerifier.EnsureMayAuthenticate", ErrorCodes.UserBlocked, "Account is blocked");
            throw new ForbiddenException(ErrorCodes.UserBlocked, "This user is blocked.", "An administrator has to unblock the account.");
        }

        if (user.IsLockedAt(now))
        {
            await FailAsync(user, "Account is temporarily locked.", ct);
            CallTrace.Fail(Layer, "CredentialVerifier.EnsureMayAuthenticate", ErrorCodes.UserLocked, "Account is temporarily locked");
            throw AlreadyLockedException(user);
        }
    }

    /// <summary>A compact view of a USERS entity for the trace — the identity and state, not every column or the hash.</summary>
    private static object Summary(USERS user) => new
    {
        id = user.Id,
        userName = user.UserName,
        isActive = user.IsActive,
        isBlocked = user.IsBlocked,
        failedLoginAttempts = user.FailedLoginAttempts,
        lockedUntil = user.LockedUntil,
    };

    /// <summary>A wrong password or wrong security answer: counted towards the lock, audited and committed.</summary>
    public async Task RegisterWrongAttemptAsync(USERS user, SecuritySettings settings, DateTime now, string what, CancellationToken ct)
    {
        using var step = CallTrace.Step(Layer, "CredentialVerifier.RegisterWrongAttempt",
            new { user = Summary(user), settings, now, what });

        // The decision first (domain, in memory) …
        var lockedNow = user.RegisterFailedLogin(now, settings.FailedLoginLockEnabled,
            settings.MaxFailedLoginAttempts, settings.AccountLockDurationMinutes);
        CallTrace.Note("Domain", "USERS.RegisterFailedLogin",
            input: new
            {
                now,
                lockEnabled = settings.FailedLoginLockEnabled,
                maxAttempts = settings.MaxFailedLoginAttempts,
                lockMinutes = settings.AccountLockDurationMinutes,
            },
            output: new { attempts = user.FailedLoginAttempts, locked = lockedNow });

        await _log.WriteAsync(UserLogEntry.Failed(UserLogActions.LoginFailed, UserLogModules.Auth,
            $"{what} (attempt {user.FailedLoginAttempts}).", EntityName, user.Id.ToString(), actorUserId: user.Id), ct);

        if (lockedNow)
        {
            await _log.WriteAsync(UserLogEntry.Success(UserLogActions.AccountLocked, UserLogModules.Auth,
                EntityName, user.Id.ToString(),
                $"Locked until {user.LockedUntil:yyyy-MM-dd HH:mm} after {user.FailedLoginAttempts} failed attempts.",
                actorUserId: user.Id), ct);
        }

        // … then the persist (the SQL nests under this step, after the decision above).
        await _users.SaveChangesAsync(ct);
        step?.Output(new { attempts = user.FailedLoginAttempts, locked = lockedNow });
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
