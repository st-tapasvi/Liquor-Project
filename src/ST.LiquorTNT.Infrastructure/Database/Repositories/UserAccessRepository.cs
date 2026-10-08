using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class UserAccessRepository : IUserAccessRepository
{
    private const string AllSupplierCodes = "All supplier codes";

    private readonly AppDbContext _db;

    public UserAccessRepository(AppDbContext db) => _db = db;

    public async Task<UserAccessResponse> GetAccessAsync(USERS user, CancellationToken ct)
    {
        var supplierCodeNames = await SupplierCodeNamesAsync(user.Id, ct);

        var roles = await (from ur in _db.USER_ROLES.AsNoTracking()
                           join r in _db.ROLES on ur.RoleId equals r.Id
                           where ur.UserId == user.Id
                           select new { ur.RoleId, r.RoleName, r.SupplierCodeId })
                          .ToListAsync(ct);

        var rights = await (from uri in _db.USER_RIGHTS.AsNoTracking()
                            join pa in _db.PAGE_ACTIONS on uri.PageActionId equals pa.Id
                            where uri.UserId == user.Id
                            orderby pa.PermissionKey
                            select new { uri.PageActionId, pa.PermissionKey, pa.ActionName, uri.SupplierCodeId })
                           .ToListAsync(ct);

        return new UserAccessResponse
        {
            UserId = user.Id,
            UserName = user.UserName,
            Roles = roles.Select(r => new UserRoleResponse
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                DisplayName = RoleNames.Display(r.RoleName, r.SupplierCodeId is null ? null : Name(supplierCodeNames, r.SupplierCodeId)),
                SupplierCodeId = r.SupplierCodeId,
                SupplierCodeName = Name(supplierCodeNames, r.SupplierCodeId),
            })
            .OrderBy(r => r.SupplierCodeId is null ? 0 : 1).ThenBy(r => r.SupplierCodeName).ThenBy(r => r.RoleName)
            .ToList(),
            Rights = rights.Select(r => new UserRightResponse
            {
                PageActionId = r.PageActionId,
                PermissionKey = r.PermissionKey,
                ActionName = r.ActionName,
                SupplierCodeId = r.SupplierCodeId,
                SupplierCodeName = Name(supplierCodeNames, r.SupplierCodeId),
            }).ToList(),
        };
    }

    public async Task<IReadOnlyList<UserRoleAssignment>> GetRoleAssignmentsAsync(int userId, CancellationToken ct) =>
        await _db.USER_ROLES.AsNoTracking().Where(r => r.UserId == userId)
            .Select(r => new UserRoleAssignment { RoleId = r.RoleId })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UserRightAssignment>> GetRightAssignmentsAsync(int userId, CancellationToken ct) =>
        await _db.USER_RIGHTS.AsNoTracking().Where(r => r.UserId == userId)
            .Select(r => new UserRightAssignment { PageActionId = r.PageActionId, SupplierCodeId = r.SupplierCodeId })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ROLES>> GetRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct) =>
        await _db.ROLES.AsNoTracking().Where(r => roleIds.Contains(r.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<PAGE_ACTIONS>> GetPageActionsAsync(IReadOnlyCollection<int> pageActionIds, CancellationToken ct) =>
        await _db.PAGE_ACTIONS.AsNoTracking().Where(a => pageActionIds.Contains(a.Id) && a.IsActive).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> GetSupplierCodeCompaniesAsync(IReadOnlyCollection<int> supplierCodeIds, CancellationToken ct) =>
        await _db.SUPPLIER_CODE.AsNoTracking()
            .Where(s => supplierCodeIds.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, s => s.CompanyId, ct);

    public Task<bool> IsAdminUserAsync(int userId, CancellationToken ct) =>
        (from ur in _db.USER_ROLES
         join r in _db.ROLES on ur.RoleId equals r.Id
         where ur.UserId == userId && (r.IsAdminRole || r.IsSystem)
         select ur.Id)
        .AnyAsync(ct);

    public async Task ReplaceRolesAsync(int userId, IReadOnlyCollection<UserRoleAssignment> roles, DateTime now, int? changedBy, CancellationToken ct)
    {
        var existing = await _db.USER_ROLES.Where(r => r.UserId == userId).ToListAsync(ct);
        var wanted = roles.Select(r => r.RoleId).ToHashSet();

        _db.USER_ROLES.RemoveRange(existing.Where(r => !wanted.Contains(r.RoleId)));

        var have = existing.Select(r => r.RoleId).ToHashSet();
        foreach (var roleId in wanted.Where(id => !have.Contains(id)))
        {
            await _db.USER_ROLES.AddAsync(USER_ROLES.Create(userId, roleId, now, changedBy), ct);
        }
    }

    public async Task ReplaceRightsAsync(int userId, IReadOnlyCollection<UserRightAssignment> rights, DateTime now, int? changedBy, CancellationToken ct)
    {
        var existing = await _db.USER_RIGHTS.Where(r => r.UserId == userId).ToListAsync(ct);
        var wanted = rights.Select(r => (r.PageActionId, r.SupplierCodeId)).ToHashSet();

        _db.USER_RIGHTS.RemoveRange(existing.Where(r => !wanted.Contains((r.PageActionId, r.SupplierCodeId))));

        var have = existing.Select(r => (r.PageActionId, r.SupplierCodeId)).ToHashSet();
        foreach (var (pageActionId, supplierCodeId) in wanted.Where(w => !have.Contains(w)))
        {
            await _db.USER_RIGHTS.AddAsync(USER_RIGHTS.Create(userId, pageActionId, supplierCodeId, now, changedBy), ct);
        }
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    /// <summary>Display names of every supplier code named on the user's roles or rights.</summary>
    private async Task<Dictionary<int, string>> SupplierCodeNamesAsync(int userId, CancellationToken ct)
    {
        var ids = (from ur in _db.USER_ROLES
                   join r in _db.ROLES on ur.RoleId equals r.Id
                   where ur.UserId == userId && r.SupplierCodeId != null
                   select r.SupplierCodeId!.Value)
            .Union(_db.USER_RIGHTS.Where(r => r.UserId == userId && r.SupplierCodeId != null).Select(r => r.SupplierCodeId!.Value));

        var supplierCodes = await SupplierCodeQuery.ToResponsesAsync(_db, _db.SUPPLIER_CODE.Where(s => ids.Contains(s.Id)), ct);
        return supplierCodes.ToDictionary(s => s.Id, s => s.DisplayName);
    }

    private static string Name(IReadOnlyDictionary<int, string> names, int? supplierCodeId) =>
        supplierCodeId is int id ? names.GetValueOrDefault(id, $"Supplier code {id}") : AllSupplierCodes;
}
