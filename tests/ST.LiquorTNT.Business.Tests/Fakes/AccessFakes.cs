using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Domain.Rules;

namespace ST.LiquorTNT.Business.Tests.Fakes;

/// <summary>The session's company / supplier code, set by hand per test.</summary>
internal sealed class FakeTenantContext : ITenantContext
{
    public int? CompanyId { get; set; }
    public int? SupplierCodeId { get; set; }
    public string? ExciseCode { get; set; }
}

/// <summary>Who holds what: Super Admins, permission keys per (user, supplier code), supplier codes per user.</summary>
internal sealed class FakeAccessRepository : IAccessRepository
{
    public HashSet<int> SuperAdmins { get; } = new();
    public Dictionary<(int UserId, int OwnSupplierCode), HashSet<string>> Keys { get; } = new();
    public Dictionary<int, List<SupplierCodeResponse>> SupplierCodesByUser { get; } = new();
    public List<SupplierCodeResponse> AllSupplierCodes { get; } = new();

    public void Grant(int userId, int supplierCode, params string[] keys)
    {
        if (!Keys.TryGetValue((userId, supplierCode), out var set))
        {
            Keys[(userId, supplierCode)] = set = new HashSet<string>();
        }

        set.UnionWith(keys);
    }

    public static SupplierCodeResponse SupplierCode(int id, int companyId = 1, string excise = "RJ", string code = "550") => new()
    {
        Id = id,
        CompanyId = companyId,
        ExciseId = 2,
        ExciseCode = excise,
        SupplierCode = code,
        LiquorCategoryCode = "CL",
        DisplayName = $"{excise} CL {code}",
    };

    public Task<bool> IsSuperAdminAsync(int userId, CancellationToken ct) => Task.FromResult(SuperAdmins.Contains(userId));

    public Task<IReadOnlyList<string>> GetPermissionKeysAsync(int userId, int supplierCodeId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(Keys.TryGetValue((userId, supplierCodeId), out var set) ? set.ToList() : new List<string>());

    public Task<IReadOnlyList<string>> GetAllPermissionKeysAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(new List<string> { Permissions.UserView, Permissions.UserManageAdmin });

    public Task<IReadOnlyList<SupplierCodeResponse>> GetSupplierCodesForUserAsync(int userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SupplierCodeResponse>>(SupplierCodesByUser.TryGetValue(userId, out var list) ? list : new List<SupplierCodeResponse>());

    public Task<IReadOnlyList<SupplierCodeResponse>> GetAllSupplierCodesAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SupplierCodeResponse>>(AllSupplierCodes);

    public Task<SupplierCodeResponse?> GetSupplierCodeAsync(int supplierCodeId, CancellationToken ct) =>
        Task.FromResult(AllSupplierCodes.Concat(SupplierCodesByUser.Values.SelectMany(l => l)).FirstOrDefault(s => s.Id == supplierCodeId));
}

/// <summary>In-memory USER_ROLES / USER_RIGHTS plus the roles, page actions and supplier codes they point to.</summary>
internal sealed class FakeUserAccessRepository : IUserAccessRepository
{
    public List<ROLES> Roles { get; } = new();
    public List<PAGE_ACTIONS> Actions { get; } = new();
    public Dictionary<int, int> SupplierCodeCompany { get; } = new();
    public Dictionary<int, List<UserRoleAssignment>> UserRoles { get; } = new();
    public Dictionary<int, List<UserRightAssignment>> UserRights { get; } = new();
    public int SaveCount { get; private set; }

    public Task<UserAccessResponse> GetAccessAsync(USERS user, CancellationToken ct) => Task.FromResult(new UserAccessResponse
    {
        UserId = user.Id,
        UserName = user.UserName,
        Roles = UserRoles.GetValueOrDefault(user.Id, new()).Select(r => new UserRoleResponse { RoleId = r.RoleId, SupplierCodeId = r.SupplierCodeId }).ToList(),
        Rights = UserRights.GetValueOrDefault(user.Id, new()).Select(r => new UserRightResponse { PageActionId = r.PageActionId, SupplierCodeId = r.SupplierCodeId }).ToList(),
    });

    public Task<IReadOnlyList<UserRoleAssignment>> GetRoleAssignmentsAsync(int userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserRoleAssignment>>(UserRoles.GetValueOrDefault(userId, new()));

    public Task<IReadOnlyList<UserRightAssignment>> GetRightAssignmentsAsync(int userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserRightAssignment>>(UserRights.GetValueOrDefault(userId, new()));

    public Task<IReadOnlyList<ROLES>> GetRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ROLES>>(Roles.Where(r => roleIds.Contains(r.Id)).ToList());

    public Task<IReadOnlyList<PAGE_ACTIONS>> GetPageActionsAsync(IReadOnlyCollection<int> pageActionIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PAGE_ACTIONS>>(Actions.Where(a => pageActionIds.Contains(a.Id)).ToList());

    public Task<IReadOnlyDictionary<int, int>> GetSupplierCodeCompaniesAsync(IReadOnlyCollection<int> supplierCodeIds, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(SupplierCodeCompany.Where(l => supplierCodeIds.Contains(l.Key)).ToDictionary(l => l.Key, l => l.Value));

    public Task<bool> IsAdminUserAsync(int userId, CancellationToken ct) =>
        Task.FromResult(UserRoles.GetValueOrDefault(userId, new())
            .Any(a => Roles.FirstOrDefault(r => r.Id == a.RoleId) is { } role && (role.IsAdminRole || role.IsSystem)));

    public Task ReplaceRolesAsync(int userId, IReadOnlyCollection<UserRoleAssignment> roles, DateTime now, int? changedBy, CancellationToken ct)
    {
        UserRoles[userId] = roles.ToList();
        return Task.CompletedTask;
    }

    public Task ReplaceRightsAsync(int userId, IReadOnlyCollection<UserRightAssignment> rights, DateTime now, int? changedBy, CancellationToken ct)
    {
        UserRights[userId] = rights.ToList();
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Rows that are seeded by SQL in production (roles, page actions) built by reflection.</summary>
internal static class AccessRows
{
    public static ROLES Role(int id, int? companyId, string name, bool isAdminRole = false, bool isSystem = false, bool isTemplate = false)
    {
        var role = ROLES.Create(companyId, name, null, isAdminRole, TestData.Now, null).WithId(id);
        Set(role, nameof(ROLES.IsSystem), isSystem);
        Set(role, nameof(ROLES.IsTemplate), isTemplate);
        return role;
    }

    public static PAGE_ACTIONS Action(int id, string permissionKey, GrantScope scope = GrantScope.ANY)
    {
        var action = (PAGE_ACTIONS)Activator.CreateInstance(typeof(PAGE_ACTIONS), nonPublic: true)!;
        Set(action, nameof(PAGE_ACTIONS.PermissionKey), permissionKey);
        Set(action, nameof(PAGE_ACTIONS.ActionKey), permissionKey.Split('.')[1]);
        Set(action, nameof(PAGE_ACTIONS.GrantScope), scope);
        Set(action, nameof(PAGE_ACTIONS.IsActive), true);
        return action.WithId(id);
    }

    private static void Set(object target, string property, object value) =>
        target.GetType().GetProperty(property)!.SetValue(target, value);
}
