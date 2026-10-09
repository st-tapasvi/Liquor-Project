using ST.LiquorTNT.Contracts.RoleGroups;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.RoleGroups;

/// <summary>
/// Data access for role groups and the roles inside them. Declared here because the RoleGroups feature owns it;
/// implemented in ST.LiquorTNT.Infrastructure.
/// </summary>
public interface IRoleGroupRepository
{
    Task<ROLE_GROUP?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>The groups of one company with their roles and user count, by name.</summary>
    Task<IReadOnlyList<RoleGroupResponse>> GetListAsync(int companyId, CancellationToken ct);

    /// <summary>One group projected like the list.</summary>
    Task<RoleGroupResponse?> GetResponseAsync(int id, CancellationToken ct);

    /// <summary>Same name already used by another group of the company? Case-insensitive.</summary>
    Task<bool> NameExistsAsync(int companyId, string groupName, int? excludeGroupId, CancellationToken ct);

    /// <summary>The roles with these ids (unknown ids are missing from the result).</summary>
    Task<IReadOnlyList<ROLES>> GetRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct);

    /// <summary>The role ids inside the group.</summary>
    Task<IReadOnlyList<int>> GetRoleIdsAsync(int groupId, CancellationToken ct);

    /// <summary>Makes the group's roles exactly <paramref name="roleIds"/>: missing rows are added, extra rows removed.</summary>
    Task ReplaceRolesAsync(int groupId, IReadOnlyCollection<int> roleIds, DateTime now, int? changedBy, CancellationToken ct);

    /// <summary>True when at least one user holds the group.</summary>
    Task<bool> IsAssignedAsync(int groupId, CancellationToken ct);

    /// <summary>True when <paramref name="userId"/> holds the group (nobody but Admin changes a group they hold).</summary>
    Task<bool> IsMemberAsync(int groupId, int userId, CancellationToken ct);

    /// <summary>True when an admin user (holder of an admin or system role, directly or through any group) holds the group.</summary>
    Task<bool> HasAdminMemberAsync(int groupId, CancellationToken ct);

    Task AddAsync(ROLE_GROUP group, CancellationToken ct);

    /// <summary>Deletes the group together with its role rows.</summary>
    Task RemoveAsync(ROLE_GROUP group, CancellationToken ct);

    /// <summary>Saves; a duplicate group name (unique key) becomes 409 ROLE_GROUP_NAME_TAKEN.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
