using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Auth;

/// <summary>USER_SESSION data access. Declared here because Auth owns sessions; implemented in Infrastructure.</summary>
public interface ISessionRepository
{
    Task AddAsync(USER_SESSION session, CancellationToken ct);

    Task<USER_SESSION?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);

    Task<USER_SESSION?> GetByIdForUserAsync(int sessionId, int userId, CancellationToken ct);

    /// <summary>Sessions that are ACTIVE and not yet expired at <paramref name="now"/>.</summary>
    Task<IReadOnlyList<USER_SESSION>> GetActiveForUserAsync(int userId, DateTime now, CancellationToken ct);

    Task<int> CountActiveAsync(int userId, DateTime now, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
