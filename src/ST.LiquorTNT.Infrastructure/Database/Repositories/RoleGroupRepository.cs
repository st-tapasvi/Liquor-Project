using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.RoleGroups;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Contracts.RoleGroups;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class RoleGroupRepository : IRoleGroupRepository
{
    private readonly AppDbContext _db;

    public RoleGroupRepository(AppDbContext db) => _db = db;

    public Task<ROLE_GROUP?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.ROLE_GROUP.FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IReadOnlyList<RoleGroupResponse>> GetListAsync(int companyId, CancellationToken ct) =>
        await ProjectAsync(_db.ROLE_GROUP.AsNoTracking().Where(g => g.CompanyId == companyId), ct);

    public async Task<RoleGroupResponse?> GetResponseAsync(int id, CancellationToken ct) =>
        (await ProjectAsync(_db.ROLE_GROUP.AsNoTracking().Where(g => g.Id == id), ct)).FirstOrDefault();

    public Task<bool> NameExistsAsync(int companyId, string groupName, int? excludeGroupId, CancellationToken ct) =>
        _db.ROLE_GROUP.AnyAsync(g => g.CompanyId == companyId && g.GroupName == groupName && g.Id != excludeGroupId, ct);

    public async Task<IReadOnlyList<ROLES>> GetRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct) =>
        await _db.ROLES.AsNoTracking().Where(r => roleIds.Contains(r.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<int>> GetRoleIdsAsync(int groupId, CancellationToken ct) =>
        await _db.ROLE_GROUP_ROLES.AsNoTracking().Where(r => r.RoleGroupId == groupId).Select(r => r.RoleId).ToListAsync(ct);

    public async Task ReplaceRolesAsync(int groupId, IReadOnlyCollection<int> roleIds, DateTime now, int? changedBy, CancellationToken ct)
    {
        var existing = await _db.ROLE_GROUP_ROLES.Where(r => r.RoleGroupId == groupId).ToListAsync(ct);

        _db.ROLE_GROUP_ROLES.RemoveRange(existing.Where(r => !roleIds.Contains(r.RoleId)));

        var have = existing.Select(r => r.RoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(id => !have.Contains(id)))
        {
            await _db.ROLE_GROUP_ROLES.AddAsync(ROLE_GROUP_ROLES.Create(groupId, roleId, now, changedBy), ct);
        }
    }

    public Task<bool> IsAssignedAsync(int groupId, CancellationToken ct) =>
        _db.USER_ROLE_GROUPS.AnyAsync(u => u.RoleGroupId == groupId, ct);

    public Task<bool> IsMemberAsync(int groupId, int userId, CancellationToken ct) =>
        _db.USER_ROLE_GROUPS.AnyAsync(u => u.RoleGroupId == groupId && u.UserId == userId, ct);

    public Task<bool> HasAdminMemberAsync(int groupId, CancellationToken ct)
    {
        // users of the group who hold an admin or system role directly ...
        var direct =
            from ug in _db.USER_ROLE_GROUPS
            join ur in _db.USER_ROLES on ug.UserId equals ur.UserId
            join r in _db.ROLES on ur.RoleId equals r.Id
            where ug.RoleGroupId == groupId && (r.IsAdminRole || r.IsSystem)
            select ug.UserId;

        // ... or an admin role through any of their groups (this one included)
        var throughGroups =
            from ug in _db.USER_ROLE_GROUPS
            join other in _db.USER_ROLE_GROUPS on ug.UserId equals other.UserId
            join gr in _db.ROLE_GROUP_ROLES on other.RoleGroupId equals gr.RoleGroupId
            join r in _db.ROLES on gr.RoleId equals r.Id
            where ug.RoleGroupId == groupId && r.IsAdminRole
            select ug.UserId;

        return direct.Union(throughGroups).AnyAsync(ct);
    }

    public async Task AddAsync(ROLE_GROUP group, CancellationToken ct) => await _db.ROLE_GROUP.AddAsync(group, ct);

    public async Task RemoveAsync(ROLE_GROUP group, CancellationToken ct)
    {
        // ROLE_GROUP_ROLES cascade in the database; removed here too so EF's view stays consistent.
        _db.ROLE_GROUP_ROLES.RemoveRange(await _db.ROLE_GROUP_ROLES.Where(r => r.RoleGroupId == group.Id).ToListAsync(ct));
        _db.ROLE_GROUP.Remove(group);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // Two requests passed the name check at the same moment; the unique key is the final arbiter.
            throw new BusinessException(ErrorCodes.RoleGroupNameTaken, "A role group with this name already exists.");
        }
    }

    /// <summary>Groups with their roles ("Operator RJ CL 772"), admin flag and user count, by name.</summary>
    private async Task<List<RoleGroupResponse>> ProjectAsync(IQueryable<ROLE_GROUP> groups, CancellationToken ct)
    {
        var rows = await groups.OrderBy(g => g.GroupName).Select(g => new RoleGroupResponse
        {
            Id = g.Id,
            CompanyId = g.CompanyId,
            GroupName = g.GroupName,
            Description = g.Description,
            IsActive = g.IsActive,
            UserCount = _db.USER_ROLE_GROUPS.Count(u => u.RoleGroupId == g.Id),
        }).ToListAsync(ct);

        if (rows.Count == 0)
        {
            return rows;
        }

        var ids = rows.Select(r => r.Id).ToList();
        var roles = await (from gr in _db.ROLE_GROUP_ROLES.AsNoTracking()
                           join r in _db.ROLES on gr.RoleId equals r.Id
                           where ids.Contains(gr.RoleGroupId)
                           select new { gr.RoleGroupId, r.Id, r.RoleName, r.SupplierCodeId, r.IsAdminRole })
                          .ToListAsync(ct);

        var codeIds = roles.Where(r => r.SupplierCodeId != null).Select(r => r.SupplierCodeId!.Value).Distinct().ToList();
        var codeNames = codeIds.Count == 0
            ? new Dictionary<int, string>()
            : (await SupplierCodeQuery.ToResponsesAsync(_db, _db.SUPPLIER_CODE.Where(s => codeIds.Contains(s.Id)), ct))
                .ToDictionary(s => s.Id, s => s.DisplayName);

        var byGroup = roles.ToLookup(r => r.RoleGroupId);
        foreach (var row in rows)
        {
            row.Roles = byGroup[row.Id]
                .Select(r =>
                {
                    var codeName = r.SupplierCodeId is int id ? codeNames.GetValueOrDefault(id) : null;
                    return new RoleGroupRoleResponse
                    {
                        RoleId = r.Id,
                        RoleName = r.RoleName,
                        DisplayName = RoleNames.Display(r.RoleName, codeName),
                        SupplierCodeId = r.SupplierCodeId,
                        SupplierCodeName = codeName,
                        IsAdminRole = r.IsAdminRole,
                    };
                })
                .OrderBy(r => r.SupplierCodeId is null ? 0 : 1).ThenBy(r => r.SupplierCodeName).ThenBy(r => r.RoleName)
                .ToList();
            row.HasAdminRole = row.Roles.Any(r => r.IsAdminRole);
        }

        return rows;
    }
}
