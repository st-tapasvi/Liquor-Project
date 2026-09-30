using System.Reflection;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.SecurityConfig;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Tests.Fakes;

/// <summary>Rows that are seeded by SQL in production (questions, config) are built by reflection here.</summary>
internal static class SeedRows
{
    public static SECURITY_QUESTION Question(int id, string text, bool active = true)
    {
        var q = (SECURITY_QUESTION)Activator.CreateInstance(typeof(SECURITY_QUESTION), nonPublic: true)!;
        Set(q, nameof(SECURITY_QUESTION.QuestionText), text);
        Set(q, nameof(SECURITY_QUESTION.Status), active);
        return q.WithId(id);
    }

    public static SECURITY_CONFIG Config(string key, string value, string dataType, int id = 1)
    {
        var c = (SECURITY_CONFIG)Activator.CreateInstance(typeof(SECURITY_CONFIG), nonPublic: true)!;
        Set(c, nameof(SECURITY_CONFIG.ConfigKey), key);
        Set(c, nameof(SECURITY_CONFIG.ConfigValue), value);
        Set(c, nameof(SECURITY_CONFIG.DataType), dataType);
        Set(c, nameof(SECURITY_CONFIG.IsActive), true);
        return c.WithId(id);
    }

    private static void Set(object target, string property, object value) =>
        target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)!.SetValue(target, value);
}

internal sealed class FakeSecurityQuestionRepository : ISecurityQuestionRepository
{
    public List<SECURITY_QUESTION> Questions { get; } = new();
    public List<USER_SECURITY_QUESTION> UserQuestions { get; } = new();
    public List<PASSWORD_RESET_REQUEST> ResetRequests { get; } = new();
    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<SECURITY_QUESTION>> GetActiveQuestionsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SECURITY_QUESTION>>(Questions.Where(q => q.Status).ToList());

    public Task<SECURITY_QUESTION?> GetQuestionAsync(int questionId, CancellationToken ct) =>
        Task.FromResult(Questions.FirstOrDefault(q => q.Id == questionId));

    public Task<USER_SECURITY_QUESTION?> GetActiveForUserAsync(int userId, CancellationToken ct) =>
        Task.FromResult(UserQuestions.FirstOrDefault(q => q.UserId == userId && q.IsActive));

    public Task<USER_SECURITY_QUESTION?> GetForUserAndQuestionAsync(int userId, int questionId, CancellationToken ct) =>
        Task.FromResult(UserQuestions.FirstOrDefault(q => q.UserId == userId && q.QuestionId == questionId));

    public Task AddUserQuestionAsync(USER_SECURITY_QUESTION question, CancellationToken ct)
    {
        // mimic UQ_USER_QUESTION (USER_ID, QUESTION_ID) so the fake fails where the database would
        if (UserQuestions.Any(q => q.UserId == question.UserId && q.QuestionId == question.QuestionId))
        {
            throw new InvalidOperationException("Duplicate (USER_ID, QUESTION_ID) — the database would reject this insert.");
        }

        question.WithId(UserQuestions.Count + 1);
        UserQuestions.Add(question);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PASSWORD_RESET_REQUEST>> GetOpenResetRequestsAsync(int userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PASSWORD_RESET_REQUEST>>(ResetRequests.Where(r => r.UserId == userId && r.IsOpen).ToList());

    public Task<PASSWORD_RESET_REQUEST?> GetResetRequestByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        Task.FromResult(ResetRequests.FirstOrDefault(r => r.RequestTokenHash == tokenHash));

    public Task AddResetRequestAsync(PASSWORD_RESET_REQUEST request, CancellationToken ct)
    {
        request.WithId(ResetRequests.Count + 1);
        ResetRequests.Add(request);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeSecurityConfigRepository : ISecurityConfigRepository
{
    public List<SECURITY_CONFIG> Rows { get; } = new();
    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<SECURITY_CONFIG>> GetAllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SECURITY_CONFIG>>(Rows);

    public Task<SECURITY_CONFIG?> GetByKeyAsync(string key, CancellationToken ct) =>
        Task.FromResult(Rows.FirstOrDefault(r => r.ConfigKey == key));

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
