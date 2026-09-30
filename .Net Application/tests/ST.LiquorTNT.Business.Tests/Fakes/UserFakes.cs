using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Tests.Fakes;

/// <summary>In-memory USERS store. Mimics the DB: case-insensitive user names, generated ids.</summary>
internal sealed class FakeUserRepository : IUserRepository
{
    public List<USERS> Users { get; } = new();
    public int SaveCount { get; private set; }
    public (string? Search, int Page, int PageSize)? LastPageQuery { get; private set; }

    public FakeUserRepository(params USERS[] users) => Users.AddRange(users);

    public Task<USERS?> GetByIdAsync(int id, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<USERS?> GetByUserNameAsync(string userName, CancellationToken ct) =>
        Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> UserNameExistsAsync(string userName, CancellationToken ct) =>
        Task.FromResult(Users.Any(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase)));

    public Task<PagedResponse<UserResponse>> GetPageAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        LastPageQuery = (search, page, pageSize);
        var items = Users.OrderBy(u => u.UserName).Skip((page - 1) * pageSize).Take(pageSize).Select(UserProjections.Map).ToList();
        return Task.FromResult(new PagedResponse<UserResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = Users.Count });
    }

    public Task<IReadOnlyList<string>> GetRecentPasswordHashesAsync(int userId, int count, CancellationToken ct)
    {
        var user = Users.FirstOrDefault(u => u.Id == userId);
        IReadOnlyList<string> hashes = user is null || count <= 0
            ? Array.Empty<string>()
            : user.PasswordHistory.OrderByDescending(h => h.CreatedAt).Take(count).Select(h => h.PasswordHash).ToList();
        return Task.FromResult(hashes);
    }

    public Task AddAsync(USERS user, CancellationToken ct)
    {
        user.WithId(Users.Count == 0 ? 1 : Users.Max(u => u.Id) + 1);
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeReferenceLookup : IReferenceLookup
{
    public HashSet<int> Roles { get; } = new() { 1 };
    public HashSet<int> Companies { get; } = new() { 1 };
    public int CompanyChecks { get; private set; }

    public Task<bool> RoleExistsAsync(int roleId, CancellationToken ct) => Task.FromResult(Roles.Contains(roleId));

    public Task<bool> CompanyExistsAsync(int companyId, CancellationToken ct)
    {
        CompanyChecks++;
        return Task.FromResult(Companies.Contains(companyId));
    }
}

internal sealed class FakePasswordPolicyRepository : IPasswordPolicyRepository
{
    public Dictionary<int, PASSWORD_POLICY> ByRole { get; } = new();
    public List<PASSWORD_POLICY> Policies { get; } = new();
    public int SaveCount { get; private set; }

    public FakePasswordPolicyRepository(PASSWORD_POLICY? policyForRole1 = null)
    {
        if (policyForRole1 is not null)
        {
            ByRole[1] = policyForRole1;
            Policies.Add(policyForRole1);
        }
    }

    public Task<PASSWORD_POLICY?> GetForRoleAsync(int roleId, CancellationToken ct) =>
        Task.FromResult(ByRole.TryGetValue(roleId, out var p) ? p : null);

    public Task<PASSWORD_POLICY?> GetByIdAsync(int id, CancellationToken ct) =>
        Task.FromResult(Policies.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<PASSWORD_POLICY>> GetAllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PASSWORD_POLICY>>(Policies);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
