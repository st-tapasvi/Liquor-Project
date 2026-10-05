using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>Forgot-password data: questions, a user's chosen question, reset requests. Implemented in Infrastructure.</summary>
public interface ISecurityQuestionRepository
{
    Task<IReadOnlyList<SECURITY_QUESTION>> GetActiveQuestionsAsync(CancellationToken ct);

    Task<SECURITY_QUESTION?> GetQuestionAsync(int questionId, CancellationToken ct);

    Task<USER_SECURITY_QUESTION?> GetActiveForUserAsync(int userId, CancellationToken ct);

    /// <summary>The user's row for one question, active or not (the pair is unique).</summary>
    Task<USER_SECURITY_QUESTION?> GetForUserAndQuestionAsync(int userId, int questionId, CancellationToken ct);

    Task AddUserQuestionAsync(USER_SECURITY_QUESTION question, CancellationToken ct);

    Task<IReadOnlyList<PASSWORD_RESET_REQUEST>> GetOpenResetRequestsAsync(int userId, CancellationToken ct);

    Task<PASSWORD_RESET_REQUEST?> GetResetRequestByTokenHashAsync(string tokenHash, CancellationToken ct);

    Task AddResetRequestAsync(PASSWORD_RESET_REQUEST request, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
