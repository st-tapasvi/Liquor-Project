using ST.LiquorTNT.Business.RoleGroups;
using ST.LiquorTNT.Contracts.RoleGroups;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Tests.Fakes;

/// <summary>In-memory role groups, their roles and members.</summary>
internal sealed class FakeRoleGroupRepository : IRoleGroupRepository
{
    public List<ROLE_GROUP> Groups { get; } = new();
    public List<ROLES> Roles { get; } = new();
    public Dictionary<int, HashSet<int>> GroupRoles { get; } = new();
    public Dictionary<int, HashSet<int>> Members { get; } = new();

    /// <summary>Groups held by at least one admin user.</summary>
    public HashSet<int> GroupsWithAdminMember { get; } = new();
    public int SaveCount { get; private set; }

    public Task<ROLE_GROUP?> GetByIdAsync(int id, CancellationToken ct) => Task.FromResult(Groups.FirstOrDefault(g => g.Id == id));

    public Task<IReadOnlyList<RoleGroupResponse>> GetListAsync(int companyId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RoleGroupResponse>>(Groups.Where(g => g.CompanyId == companyId).Select(Project).ToList());

    public Task<RoleGroupResponse?> GetResponseAsync(int id, CancellationToken ct) =>
        Task.FromResult(Groups.Where(g => g.Id == id).Select(Project).FirstOrDefault());

    public Task<bool> NameExistsAsync(int companyId, string groupName, int? excludeGroupId, CancellationToken ct) =>
        Task.FromResult(Groups.Any(g => g.CompanyId == companyId && string.Equals(g.GroupName, groupName, StringComparison.OrdinalIgnoreCase)
                                        && g.Id != excludeGroupId));

    public Task<IReadOnlyList<ROLES>> GetRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ROLES>>(Roles.Where(r => roleIds.Contains(r.Id)).ToList());

    public Task<IReadOnlyList<int>> GetRoleIdsAsync(int groupId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<int>>(GroupRoles.GetValueOrDefault(groupId, new()).ToList());

    public Task ReplaceRolesAsync(int groupId, IReadOnlyCollection<int> roleIds, DateTime now, int? changedBy, CancellationToken ct)
    {
        GroupRoles[groupId] = roleIds.ToHashSet();
        return Task.CompletedTask;
    }

    public Task<bool> IsAssignedAsync(int groupId, CancellationToken ct) =>
        Task.FromResult(Members.GetValueOrDefault(groupId, new()).Count > 0);

    public Task<bool> IsMemberAsync(int groupId, int userId, CancellationToken ct) =>
        Task.FromResult(Members.GetValueOrDefault(groupId, new()).Contains(userId));

    public Task<bool> HasAdminMemberAsync(int groupId, CancellationToken ct) => Task.FromResult(GroupsWithAdminMember.Contains(groupId));

    public Task AddAsync(ROLE_GROUP group, CancellationToken ct)
    {
        group.WithId(Groups.Count == 0 ? 1 : Groups.Max(g => g.Id) + 1);
        Groups.Add(group);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(ROLE_GROUP group, CancellationToken ct)
    {
        Groups.Remove(group);
        GroupRoles.Remove(group.Id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private RoleGroupResponse Project(ROLE_GROUP g)
    {
        var roles = GroupRoles.GetValueOrDefault(g.Id, new())
            .Select(id => Roles.First(r => r.Id == id))
            .Select(r => new RoleGroupRoleResponse { RoleId = r.Id, RoleName = r.RoleName, DisplayName = r.RoleName, SupplierCodeId = r.SupplierCodeId, IsAdminRole = r.IsAdminRole })
            .ToList();

        return new RoleGroupResponse
        {
            Id = g.Id, CompanyId = g.CompanyId, GroupName = g.GroupName, Description = g.Description, IsActive = g.IsActive,
            Roles = roles, HasAdminRole = roles.Any(r => r.IsAdminRole), UserCount = Members.GetValueOrDefault(g.Id, new()).Count,
        };
    }
}
