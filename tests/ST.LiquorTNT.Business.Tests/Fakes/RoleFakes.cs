using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Contracts.Roles;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Tests.Fakes;

/// <summary>In-memory roles, their rights and policy links, plus the page actions of the rights grid.</summary>
internal sealed class FakeRoleRepository : IRoleRepository
{
    public List<ROLES> Roles { get; } = new();
    public Dictionary<int, HashSet<int>> Rights { get; } = new();
    public List<ROLE_PASSWORD_POLICY> PolicyLinks { get; } = new();
    public List<PAGE_ACTIONS> Actions { get; } = new();
    public HashSet<int> AssignedRoles { get; } = new();
    public HashSet<int> PolicyIds { get; } = new() { 1, 2, 3 };
    public int SaveCount { get; private set; }

    public Task<ROLES?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult(Roles.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<RoleResponse>> GetListAsync(int? companyId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RoleResponse>>(Roles.Where(r => r.CompanyId == companyId)
            .Select(r => new RoleResponse { Id = r.Id, CompanyId = r.CompanyId, RoleName = r.RoleName }).ToList());

    public Task<bool> NameExistsAsync(int? companyId, string roleName, int? excludeRoleId, CancellationToken ct) =>
        Task.FromResult(Roles.Any(r => r.CompanyId == companyId && string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase) && r.Id != excludeRoleId));

    public Task<bool> CompanyHasRolesAsync(int companyId, CancellationToken ct) => Task.FromResult(Roles.Any(r => r.CompanyId == companyId));

    public Task<IReadOnlyList<ROLES>> GetTemplatesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ROLES>>(Roles.Where(r => r.IsTemplate).ToList());

    public Task<bool> IsAssignedAsync(int roleId, CancellationToken ct) => Task.FromResult(AssignedRoles.Contains(roleId));

    public Task<IReadOnlyList<int>> GetRightIdsAsync(int roleId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<int>>(Rights.TryGetValue(roleId, out var set) ? set.ToList() : new List<int>());

    public Task ReplaceRightsAsync(int roleId, IReadOnlyCollection<int> pageActionIds, DateTime now, int? changedBy, CancellationToken ct)
    {
        Rights[roleId] = pageActionIds.ToHashSet();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PageResponse>> GetPagesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PageResponse>>(new List<PageResponse>
        {
            new()
            {
                PageId = 1, PageKey = "all", PageName = "All",
                Actions = Actions.Select(a => new PageActionResponse
                {
                    PageActionId = a.Id, PermissionKey = a.PermissionKey, GrantScope = a.GrantScope.ToString(),
                }).ToList(),
            },
        });

    public Task<IReadOnlyList<PAGE_ACTIONS>> GetPageActionsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PAGE_ACTIONS>>(Actions.Where(a => ids.Contains(a.Id)).ToList());

    public Task<ROLE_PASSWORD_POLICY?> GetPolicyLinkAsync(int roleId, CancellationToken ct) =>
        Task.FromResult(PolicyLinks.FirstOrDefault(l => l.RoleId == roleId));

    public Task<bool> PasswordPolicyExistsAsync(int passwordPolicyId, CancellationToken ct) => Task.FromResult(PolicyIds.Contains(passwordPolicyId));

    public Task AddAsync(ROLES role, CancellationToken ct)
    {
        role.WithId(Roles.Count == 0 ? 1 : Roles.Max(r => r.Id) + 1);
        Roles.Add(role);
        return Task.CompletedTask;
    }

    public Task AddPolicyLinkAsync(ROLE_PASSWORD_POLICY link, CancellationToken ct)
    {
        PolicyLinks.Add(link);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(ROLES role, CancellationToken ct)
    {
        Roles.Remove(role);
        Rights.Remove(role.Id);
        PolicyLinks.RemoveAll(l => l.RoleId == role.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
