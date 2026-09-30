using FluentValidation;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>
/// Forgot password. A locked, blocked or inactive account cannot recover (a lock is a lock — the same
/// rule as login), and every wrong answer counts as a failed login, so guessing answers locks the
/// account exactly like guessing passwords would.
/// </summary>
public sealed class PasswordResetService : IPasswordResetService
{
    private const string EntityName = "PASSWORD_RESET_REQUEST";

    private readonly ISecurityQuestionRepository _questions;
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly CredentialVerifier _credentials;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly ISecurityConfigProvider _config;
    private readonly PasswordRules _passwordRules;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<ForgotPasswordStartRequest> _startValidator;
    private readonly IValidator<ForgotPasswordVerifyRequest> _verifyValidator;
    private readonly IValidator<ForgotPasswordResetRequest> _resetValidator;

    public PasswordResetService(
        ISecurityQuestionRepository questions,
        IUserRepository users,
        ISessionRepository sessions,
        CredentialVerifier credentials,
        IPasswordHasher hasher,
        ITokenHasher tokenHasher,
        ISecurityConfigProvider config,
        PasswordRules passwordRules,
        IClock clock,
        IUserLogWriter log,
        IValidator<ForgotPasswordStartRequest> startValidator,
        IValidator<ForgotPasswordVerifyRequest> verifyValidator,
        IValidator<ForgotPasswordResetRequest> resetValidator)
    {
        _questions = questions;
        _users = users;
        _sessions = sessions;
        _credentials = credentials;
        _hasher = hasher;
        _tokenHasher = tokenHasher;
        _config = config;
        _passwordRules = passwordRules;
        _clock = clock;
        _log = log;
        _startValidator = startValidator;
        _verifyValidator = verifyValidator;
        _resetValidator = resetValidator;
    }

    public async Task<ForgotPasswordStartResponse> StartAsync(ForgotPasswordStartRequest request, CancellationToken ct)
    {
        (await _startValidator.ValidateAsync(request, ct)).EnsureValid();

        var settings = await RequireEnabledAsync(ct);
        var now = _clock.IndiaNow;
        var user = await _users.GetByUserNameAsync(request.UserName.Trim(), ct);
        var chosen = user is null ? null : await _questions.GetActiveForUserAsync(user.Id, ct);

        // Unknown user and user-without-question answer identically, so the flow reveals as little as possible.
        if (user is null || chosen is null)
        {
            throw new BusinessException(ErrorCodes.SecurityQuestionNotSet,
                "Password recovery is not available for this account.",
                "No security question is set. Ask an administrator to reset the password.");
        }

        await _credentials.EnsureMayAuthenticateAsync(user, now, ct);

        var question = await _questions.GetQuestionAsync(chosen.QuestionId, ct) ?? throw new NotFoundException("Security question");

        // one live request per user: a new start supersedes any earlier one
        foreach (var open in await _questions.GetOpenResetRequestsAsync(user.Id, ct))
        {
            open.MarkExpired(now);
        }

        var token = ResetTokens.New();
        var expiresAt = now.AddMinutes(settings.PasswordResetExpiryMinutes);
        await _questions.AddResetRequestAsync(PASSWORD_RESET_REQUEST.Create(user.Id, _tokenHasher.Hash(token), now, expiresAt), ct);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.PasswordResetRequested, UserLogModules.Auth,
            EntityName, null, $"Password reset requested, expires {expiresAt:yyyy-MM-dd HH:mm}.", actorUserId: user.Id), ct);
        await _questions.SaveChangesAsync(ct);

        return new ForgotPasswordStartResponse { RequestToken = token, QuestionText = question.QuestionText, ExpiresAt = expiresAt };
    }

    public async Task<MessageResponse> VerifyAsync(ForgotPasswordVerifyRequest request, CancellationToken ct)
    {
        (await _verifyValidator.ValidateAsync(request, ct)).EnsureValid();

        var settings = await RequireEnabledAsync(ct);
        var now = _clock.IndiaNow;
        var reset = await RequireOpenRequestAsync(request.RequestToken, PASSWORD_RESET_REQUEST.StatusPending, now, ct);
        var user = await RequireUserMayAuthenticateAsync(reset.UserId, now, ct);

        var chosen = await _questions.GetActiveForUserAsync(user.Id, ct)
                     ?? throw new BusinessException(ErrorCodes.SecurityQuestionNotSet, "No security question is set for this account.");

        if (!_hasher.Verify(SecurityAnswers.Normalize(request.Answer), chosen.AnswerHash))
        {
            var exhausted = reset.RegisterFailedVerify(now, settings.PasswordResetMaxAttempts);

            await _log.WriteAsync(UserLogEntry.Failed(UserLogActions.PasswordResetFailed, UserLogModules.Auth,
                $"Wrong security answer (attempt {reset.VerifyAttempts}).", EntityName, reset.Id.ToString(), actorUserId: user.Id), ct);
            await _questions.SaveChangesAsync(ct);

            // and it counts towards the account lock, so starting over does not buy fresh guesses
            await _credentials.RegisterWrongAttemptAsync(user, settings, now, "Wrong security answer", ct);

            if (user.IsLockedAt(now))
            {
                throw CredentialVerifier.JustLockedException(user);
            }

            throw exhausted
                ? new ForbiddenException(ErrorCodes.ResetAttemptsExceeded, "Too many wrong answers.", "Start the recovery again.")
                : new UnauthorizedException(ErrorCodes.SecurityAnswerIncorrect, "The answer is incorrect.");
        }

        reset.MarkVerified(now);
        await _questions.SaveChangesAsync(ct);

        return MessageResponse.Of($"Answer verified. Set a new password before {reset.ExpiresAt:yyyy-MM-dd HH:mm}.");
    }

    public async Task<MessageResponse> ResetAsync(ForgotPasswordResetRequest request, CancellationToken ct)
    {
        (await _resetValidator.ValidateAsync(request, ct)).EnsureValid();

        await RequireEnabledAsync(ct);
        var now = _clock.IndiaNow;
        var reset = await RequireOpenRequestAsync(request.RequestToken, PASSWORD_RESET_REQUEST.StatusVerified, now, ct);
        var user = await RequireUserMayAuthenticateAsync(reset.UserId, now, ct);

        var policy = await _passwordRules.EnsureAcceptableForUserAsync(user, request.NewPassword, ct);

        user.SetPassword(_hasher.Hash(request.NewPassword), policy.ExpiryFrom(now), forceChange: false, now, user.Id);
        user.ClearTemporaryLock(now, user.Id);      // a fresh password wipes the failed-login slate — never an admin's block

        foreach (var session in await _sessions.GetActiveForUserAsync(user.Id, now, ct))
        {
            session.Revoke(now);
        }

        reset.MarkUsed(now);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.PasswordResetSuccess, UserLogModules.Auth,
            "USERS", user.Id.ToString(), $"Password reset via security question for '{user.UserName}'.", actorUserId: user.Id), ct);
        await _sessions.SaveChangesAsync(ct);
        await _questions.SaveChangesAsync(ct);
        await _users.SaveChangesAsync(ct);

        return MessageResponse.Of("Password has been reset. Log in with the new password.");
    }

    private async Task<SecuritySettings> RequireEnabledAsync(CancellationToken ct)
    {
        var settings = await _config.GetAsync(ct);

        return settings.SecurityQuestionEnabled
            ? settings
            : throw new BusinessException(ErrorCodes.SecurityQuestionDisabled, "Password recovery by security question is disabled.");
    }

    /// <summary>The account behind a request must still be allowed to authenticate at every step, not only at start.</summary>
    private async Task<USERS> RequireUserMayAuthenticateAsync(int userId, DateTime now, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User");
        await _credentials.EnsureMayAuthenticateAsync(user, now, ct);
        return user;
    }

    /// <summary>The request behind the token must exist, be in the expected state and not be expired.</summary>
    private async Task<PASSWORD_RESET_REQUEST> RequireOpenRequestAsync(string token, string expectedStatus, DateTime now, CancellationToken ct)
    {
        var reset = await _questions.GetResetRequestByTokenHashAsync(_tokenHasher.Hash(token.Trim()), ct);

        if (reset is null || reset.Status != expectedStatus)
        {
            throw new BusinessException(ErrorCodes.ResetRequestInvalid, "This recovery request is not valid.", "Start the recovery again.");
        }

        if (reset.IsExpiredAt(now))
        {
            reset.MarkExpired(now);
            await _questions.SaveChangesAsync(ct);
            throw new BusinessException(ErrorCodes.ResetRequestExpired, "This recovery request has expired.", "Start the recovery again.");
        }

        return reset;
    }
}
