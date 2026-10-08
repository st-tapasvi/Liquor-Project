using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Domain.Rules;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// The rules about WHO may manage WHICH user and hand out WHICH role or right. Shared by user create
/// (<see cref="UserService"/>) and the access screens (<see cref="UserAccessService"/>), so both apply them identically:
/// <list type="number">
/// <item>A company user only sees and manages users of their own company (others get 404).</item>
/// <item>Admin users (Super Admin / Plant Admin holders) are managed only by a holder of user.manageadmin.</item>
/// <item>Nobody but Super Admin changes their OWN roles or rights.</item>
/// <item>Roles and supplier codes must belong to the user's company. Super Admin role is given by Super Admin only, for all supplier codes.</item>
/// <item>SYSTEM rights are never handed out; ADMIN rights and admin roles need user.manageadmin.</item>
/// </list>
/// </summary>
public sealed class UserAccessRules
{
    private readonly IUserRepository _users;
    private readonly IUserAccessRepository _access;
    private readonly CurrentAccess _current;

    public UserAccessRules(IUserRepository users, IUserAccessRepository access, CurrentAccess current)
    {
        _users = users;
        _access = access;
        _current = current;
    }

    /// <summary>The user, if it exists and is inside the caller's company (Super Admin sees everyone). Otherwise 404.</summary>
    public async Task<USERS> RequireUserAsync(int id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User");

        if (!await _current.IsSuperAdminAsync(ct) && user.CompanyId != await _current.CompanyScopeAsync(ct))
        {
            throw new NotFoundException("User");     // another company's user: do not even admit it exists
        }

        return user;
    }

    /// <summary>Changing anything about an admin user (profile, status, roles, rights) needs user.manageadmin.</summary>
    public async Task EnsureCanManageUserAsync(USERS user, CancellationToken ct)
    {
        if (await _access.IsAdminUserAsync(user.Id, ct))
        {
            await _current.EnsureCanManageAdminsAsync($"Changing the admin user '{user.UserName}'", ct);
        }
    }

    /// <summary>Nobody changes their own access (except Super Admin), so nobody can raise their own rights.</summary>
    public async Task EnsureNotSelfAsync(USERS user, CancellationToken ct)
    {
        if (user.Id == _current.UserId && !await _current.IsSuperAdminAsync(ct))
        {
            throw new BusinessException(ErrorCodes.CannotChangeOwnAccess, "You cannot change your own roles or rights.",
                "Ask another administrator to do it.");
        }
    }

    /// <summary>Checks a full list of role assignments for a user of <paramref name="companyId"/>. Returns it without duplicates.</summary>
    public async Task<IReadOnlyList<UserRoleAssignment>> ValidateRolesAsync(
        int? companyId, IReadOnlyCollection<UserRoleAssignment> requested, CancellationToken ct)
    {
        var assignments = requested
            .GroupBy(a => a.RoleId)
            .Select(g => g.First())
            .ToList();

        var roles = (await _access.GetRolesAsync(assignments.Select(a => a.RoleId).Distinct().ToList(), ct))
            .ToDictionary(r => r.Id);
        var isSuperAdmin = await _current.IsSuperAdminAsync(ct);

        foreach (var assignment in assignments)
        {
            // A role of another company, an inactive role or a template is "not found" for this user.
            if (!roles.TryGetValue(assignment.RoleId, out var role) || !role.IsActive || role.IsTemplate)
            {
                throw new NotFoundException($"Role {assignment.RoleId}");
            }

            if (role.IsSystem)
            {
                if (!isSuperAdmin)
                {
                    throw new ForbiddenException(ErrorCodes.AdminUserProtected, "Only Super Admin can give the Super Admin role.");
                }

                if (companyId is not null)
                {
                    throw new ValidationException(new Dictionary<string, string[]>
                    {
                        ["roles"] = new[] { "Super Admin works across every company: give it only to a user without a company." },
                    });
                }

                continue;
            }

            if (role.CompanyId != companyId)
            {
                throw new NotFoundException($"Role {assignment.RoleId}");
            }

            if (role.IsAdminRole)
            {
                await _current.EnsureCanManageAdminsAsync($"Giving the admin role '{role.RoleName}'", ct);
            }
        }

        // A role of a supplier code that was deactivated meanwhile cannot be given any more.
        await EnsureSupplierCodesBelongToAsync(companyId,
            assignments.Select(a => roles[a.RoleId].SupplierCodeId).Where(id => id is not null), ct);
        return assignments;
    }

    /// <summary>
    /// Checks a full list of custom rights against what the user has now. Only the CHANGES are checked for
    /// grant scope, so saving an unchanged list never fails on rights the editor could not grant.
    /// </summary>
    public async Task<IReadOnlyList<UserRightAssignment>> ValidateRightsAsync(
        int? companyId, IReadOnlyCollection<UserRightAssignment> requested, IReadOnlyCollection<UserRightAssignment> current,
        CancellationToken ct)
    {
        var assignments = requested
            .GroupBy(a => (a.PageActionId, a.SupplierCodeId))
            .Select(g => g.First())
            .ToList();

        var currentKeys = current.Select(a => (a.PageActionId, a.SupplierCodeId)).ToHashSet();
        var wantedKeys = assignments.Select(a => (a.PageActionId, a.SupplierCodeId)).ToHashSet();
        var added = assignments.Where(a => !currentKeys.Contains((a.PageActionId, a.SupplierCodeId))).Select(a => a.PageActionId);
        var removed = current.Where(a => !wantedKeys.Contains((a.PageActionId, a.SupplierCodeId))).Select(a => a.PageActionId);

        var addedIds = added.Distinct().ToList();
        var changedIds = addedIds.Concat(removed).Distinct().ToList();
        var actions = (await _access.GetPageActionsAsync(assignments.Select(a => a.PageActionId).Concat(changedIds).Distinct().ToList(), ct))
            .ToDictionary(a => a.Id);

        var unknown = assignments.Select(a => a.PageActionId).Where(id => !actions.ContainsKey(id)).Distinct().ToList();
        if (unknown.Count > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["rights"] = new[] { $"Unknown page action id(s): {string.Join(", ", unknown)}." },
            });
        }

        var system = addedIds.Where(id => actions[id].GrantScope == GrantScope.SYSTEM).Select(id => actions[id].PermissionKey).ToList();
        if (system.Count > 0)
        {
            throw new ForbiddenException(ErrorCodes.RightNotGrantable, "These rights cannot be given to a user.",
                $"{string.Join(", ", system)} belong to Super Admin only.");
        }

        var admin = changedIds.Where(id => actions.TryGetValue(id, out var a) && a.GrantScope == GrantScope.ADMIN)
                              .Select(id => actions[id].PermissionKey).ToList();
        if (admin.Count > 0)
        {
            await _current.EnsureCanManageAdminsAsync($"Giving or removing {string.Join(", ", admin)}", ct);
        }

        await EnsureSupplierCodesBelongToAsync(companyId, assignments.Select(a => a.SupplierCodeId), ct);
        return assignments;
    }

    /// <summary>Every supplier code named must be active and belong to the user's company.</summary>
    private async Task EnsureSupplierCodesBelongToAsync(int? companyId, IEnumerable<int?> supplierCodeIds, CancellationToken ct)
    {
        var ids = supplierCodeIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var companies = await _access.GetSupplierCodeCompaniesAsync(ids, ct);
        var wrong = ids.Where(id => !companies.TryGetValue(id, out var owner) || owner != companyId).ToList();

        if (wrong.Count > 0)
        {
            throw new NotFoundException($"Supplier code {string.Join(", ", wrong)}");
        }
    }
}
