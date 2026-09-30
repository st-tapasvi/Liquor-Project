using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// Data access for the User module. Declared here because the Users feature owns it;
/// implemented in ST.LiquorTNT.Infrastructure (Business may not reference Infrastructure).
/// </summary>
public interface IUserRepository
{
    Task<USERS?> GetByIdAsync(int id, CancellationToken ct);

    Task<USERS?> GetByUserNameAsync(string userName, CancellationToken ct);

    Task<bool> UserNameExistsAsync(string userName, CancellationToken ct);

    /// <summary>Server-paged list, projected to the response type in the query (never entities).</summary>
    Task<PagedResponse<UserResponse>> GetPageAsync(string? search, int page, int pageSize, CancellationToken ct);

    /// <summary>The newest <paramref name="count"/> password hashes of a user, for the reuse check.</summary>
    Task<IReadOnlyList<string>> GetRecentPasswordHashesAsync(int userId, int count, CancellationToken ct);

    Task AddAsync(USERS user, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
