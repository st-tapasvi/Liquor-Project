using FluentValidation;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Auth;

public sealed class SecurityQuestionService : ISecurityQuestionService
{
    private readonly ISecurityQuestionRepository _questions;
    private readonly CredentialVerifier _credentials;
    private readonly ISecurityConfigProvider _config;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUserLogWriter _log;
    private readonly IValidator<SetSecurityQuestionRequest> _validator;

    public SecurityQuestionService(
        ISecurityQuestionRepository questions,
        CredentialVerifier credentials,
        ISecurityConfigProvider config,
        IPasswordHasher hasher,
        IClock clock,
        ICurrentUser currentUser,
        IUserLogWriter log,
        IValidator<SetSecurityQuestionRequest> validator)
    {
        _questions = questions;
        _credentials = credentials;
        _config = config;
        _hasher = hasher;
        _clock = clock;
        _currentUser = currentUser;
        _log = log;
        _validator = validator;
    }

    public async Task<IReadOnlyList<SecurityQuestionResponse>> GetQuestionsAsync(CancellationToken ct)
    {
        var questions = await _questions.GetActiveQuestionsAsync(ct);
        return questions.Select(q => new SecurityQuestionResponse { Id = q.Id, QuestionText = q.QuestionText }).ToList();
    }

    public async Task<MessageResponse> SetMyQuestionAsync(SetSecurityQuestionRequest request, CancellationToken ct)
    {
        (await _validator.ValidateAsync(request, ct)).EnsureValid();

        var userName = _currentUser.UserName ?? throw new UnauthorizedException(ErrorCodes.SessionInvalid, "Not authenticated.");
        var settings = await _config.GetAsync(ct);
        var now = _clock.IndiaNow;

        // Same cost as a login: a hijacked session cannot guess the password here without being locked out.
        var user = await _credentials.VerifyAsync(userName, request.CurrentPassword, settings, now, ct);

        var question = await _questions.GetQuestionAsync(request.QuestionId, ct);
        if (question is null || !question.Status)
        {
            throw new NotFoundException("Security question");
        }

        var answerHash = _hasher.Hash(SecurityAnswers.Normalize(request.Answer));

        // One active question per user. A question chosen before is updated in place (UQ on USER_ID + QUESTION_ID).
        var previous = await _questions.GetActiveForUserAsync(user.Id, ct);
        if (previous is not null && previous.QuestionId != question.Id)
        {
            previous.Deactivate(now);
        }

        var existing = await _questions.GetForUserAndQuestionAsync(user.Id, question.Id, ct);
        if (existing is not null)
        {
            existing.Replace(answerHash, now);
        }
        else
        {
            await _questions.AddUserQuestionAsync(USER_SECURITY_QUESTION.Create(user.Id, question.Id, answerHash, now), ct);
        }

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.SecurityQuestionChanged, UserLogModules.Auth,
            "USER_SECURITY_QUESTION", null, $"Security question set to #{question.Id}.", actorUserId: user.Id), ct);
        await _questions.SaveChangesAsync(ct);

        return MessageResponse.Of($"Security question saved: \"{question.QuestionText}\"");
    }
}
