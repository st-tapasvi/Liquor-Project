using ST.LiquorTNT.Business.RoleGroups;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// A user's roles (USER_ROLES) and custom rights (USER_RIGHTS). Declared here because the Users feature owns
/// user access; implemented in Infrastructure.
/// </summary>
public interface IUserAccessRepository
{
    /// <summary>The user's roles and rights with names, ready for the screen.</summary>
    Task<UserAccessResponse> GetAccessAsync(USERS user, CancellationToken ct);

    Task<IReadOnlyList<UserRoleAssignment>> GetRoleAssignmentsAsync(int userId, CancellationToken ct);

    Task<IReadOnlyList<UserRightAssignment>> GetRightAssignmentsAsync(int userId, CancellationToken ct);

    /// <summary>The roles with these ids (unknown ids are missing from the result).</summary>
    Task<IReadOnlyList<ROLES>> GetRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct);

    /// <summary>The page actions with these ids (unknown ids are missing from the result).</summary>
    Task<IReadOnlyList<PAGE_ACTIONS>> GetPageActionsAsync(IReadOnlyCollection<int> pageActionIds, CancellationToken ct);

    /// <summary>Company of each ACTIVE supplier code in <paramref name="supplierCodeIds"/>, keyed by supplier code id.</summary>
    Task<IReadOnlyDictionary<int, int>> GetSupplierCodeCompaniesAsync(IReadOnlyCollection<int> supplierCodeIds, CancellationToken ct);

    /// <summary>True when the user holds Super Admin or an admin role (Plant Admin): an "admin user".</summary>
    Task<bool> IsAdminUserAsync(int userId, CancellationToken ct);

    /// <summary>Makes the user's roles exactly <paramref name="roles"/> (adds what is missing, removes the rest).</summary>
    Task ReplaceRolesAsync(int userId, IReadOnlyCollection<UserRoleAssignment> roles, DateTime now, int? changedBy, CancellationToken ct);

    /// <summary>The ids of the role groups the user holds.</summary>
    Task<IReadOnlyList<int>> GetRoleGroupIdsAsync(int userId, CancellationToken ct);

    /// <summary>The role groups with these ids, with their role ids (unknown ids are missing from the result).</summary>
    Task<IReadOnlyList<RoleGroupInfo>> GetRoleGroupsAsync(IReadOnlyCollection<int> roleGroupIds, CancellationToken ct);

    /// <summary>Makes the user's role groups exactly <paramref name="roleGroupIds"/>.</summary>
    Task ReplaceRoleGroupsAsync(int userId, IReadOnlyCollection<int> roleGroupIds, DateTime now, int? changedBy, CancellationToken ct);

    /// <summary>Makes the user's custom rights exactly <paramref name="rights"/>.</summary>
    Task ReplaceRightsAsync(int userId, IReadOnlyCollection<UserRightAssignment> rights, DateTime now, int? changedBy, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
