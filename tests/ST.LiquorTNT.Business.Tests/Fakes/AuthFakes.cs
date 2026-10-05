using System.Reflection;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Tests.Fakes;

internal static class EntityState
{
    /// <summary>Puts an entity into a state only the database could otherwise produce (e.g. legacy IS_BLOCKED).</summary>
    public static T With<T>(this T entity, string property, object? value)
    {
        typeof(T).GetProperty(property, BindingFlags.Public | BindingFlags.Instance)!.SetValue(entity, value);
        return entity;
    }
}

internal sealed class FakeSessionRepository : ISessionRepository
{
    public List<USER_SESSION> Sessions { get; } = new();
    public int SaveCount { get; private set; }

    public Task AddAsync(USER_SESSION session, CancellationToken ct)
    {
        session.WithId(Sessions.Count == 0 ? 1 : Sessions.Max(s => s.Id) + 1);
        Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<USER_SESSION?> GetByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        Task.FromResult(Sessions.FirstOrDefault(s => s.SessionTokenHash == tokenHash));

    public Task<USER_SESSION?> GetByIdForUserAsync(int sessionId, int userId, CancellationToken ct) =>
        Task.FromResult(Sessions.FirstOrDefault(s => s.Id == sessionId && s.UserId == userId));

    public Task<IReadOnlyList<USER_SESSION>> GetActiveForUserAsync(int userId, DateTime now, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<USER_SESSION>>(Sessions.Where(s => s.UserId == userId && s.IsActiveAt(now)).ToList());

    public Task<int> CountActiveAsync(int userId, DateTime now, CancellationToken ct) =>
        Task.FromResult(Sessions.Count(s => s.UserId == userId && s.IsActiveAt(now)));

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeAccessTokenService : IAccessTokenService
{
    private int _issued;

    public DateTime? LastExpiresAtUtc { get; private set; }

    public string Create(USERS user, DateTime expiresAtUtc)
    {
        LastExpiresAtUtc = expiresAtUtc;
        return $"tok-{user.UserName}-{++_issued}";
    }
}

internal sealed class FakeTokenHasher : ITokenHasher
{
    public string Hash(string token) => "TH:" + token;
}
