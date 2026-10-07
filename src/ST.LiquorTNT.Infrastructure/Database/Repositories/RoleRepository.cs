using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Roles;
using ST.LiquorTNT.Contracts.Roles;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _db;

    public RoleRepository(AppDbContext db) => _db = db;

    public Task<ROLES?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.ROLES.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<RoleResponse>> GetListAsync(int? companyId, CancellationToken ct) =>
        await (from r in _db.ROLES.AsNoTracking()
               where r.CompanyId == companyId           // EF turns this into IS NULL when companyId is null
               join link in _db.ROLE_PASSWORD_POLICY on r.Id equals link.RoleId into links
               from link in links.DefaultIfEmpty()
               orderby r.IsSystem descending, r.RoleName
               select new RoleResponse
               {
                   Id = r.Id,
                   CompanyId = r.CompanyId,
                   RoleName = r.RoleName,
                   Description = r.Description,
                   IsSystem = r.IsSystem,
                   IsTemplate = r.IsTemplate,
                   IsAdminRole = r.IsAdminRole,
                   IsActive = r.IsActive,
                   PasswordPolicyId = link == null ? null : link.PasswordPolicyId,
               })
            .ToListAsync(ct);

    public Task<bool> NameExistsAsync(int? companyId, string roleName, int? excludeRoleId, CancellationToken ct) =>
        _db.ROLES.AnyAsync(r => r.CompanyId == companyId && r.RoleName == roleName && r.Id != excludeRoleId, ct);

    public Task<bool> CompanyHasRolesAsync(int companyId, CancellationToken ct) =>
        _db.ROLES.AnyAsync(r => r.CompanyId == companyId, ct);

    public async Task<IReadOnlyList<ROLES>> GetTemplatesAsync(CancellationToken ct) =>
        await _db.ROLES.AsNoTracking().Where(r => r.IsTemplate && r.IsActive).OrderBy(r => r.Id).ToListAsync(ct);

    public Task<bool> IsAssignedAsync(int roleId, CancellationToken ct) =>
        _db.USER_ROLES.AnyAsync(r => r.RoleId == roleId, ct);

    public async Task<IReadOnlyList<int>> GetRightIdsAsync(int roleId, CancellationToken ct) =>
        await _db.ROLE_RIGHTS.AsNoTracking().Where(r => r.RoleId == roleId).Select(r => r.PageActionId).ToListAsync(ct);

    public async Task ReplaceRightsAsync(int roleId, IReadOnlyCollection<int> pageActionIds, DateTime now, int? changedBy, CancellationToken ct)
    {
        var existing = await _db.ROLE_RIGHTS.Where(r => r.RoleId == roleId).ToListAsync(ct);

        _db.ROLE_RIGHTS.RemoveRange(existing.Where(r => !pageActionIds.Contains(r.PageActionId)));

        var have = existing.Select(r => r.PageActionId).ToHashSet();
        foreach (var id in pageActionIds.Where(id => !have.Contains(id)))
        {
            await _db.ROLE_RIGHTS.AddAsync(ROLE_RIGHTS.Create(roleId, id, now, changedBy), ct);
        }
    }

    public async Task<IReadOnlyList<PageResponse>> GetPagesAsync(CancellationToken ct)
    {
        var pages = await _db.PAGES.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.SortOrder).ThenBy(p => p.PageName).ToListAsync(ct);
        var actions = await _db.PAGE_ACTIONS.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.SortOrder).ToListAsync(ct);
        var byPage = actions.ToLookup(a => a.PageId);

        return pages.Select(p => new PageResponse
        {
            PageId = p.Id,
            PageKey = p.PageKey ?? string.Empty,
            PageName = p.PageName,
            ModuleName = p.ModuleName,
            Actions = byPage[p.Id].Select(a => new PageActionResponse
            {
                PageActionId = a.Id,
                ActionKey = a.ActionKey,
                PermissionKey = a.PermissionKey,
                ActionName = a.ActionName,
                GrantScope = a.GrantScope.ToString(),
            }).ToList(),
        }).ToList();
    }

    public async Task<IReadOnlyList<PAGE_ACTIONS>> GetPageActionsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        await _db.PAGE_ACTIONS.AsNoTracking().Where(a => ids.Contains(a.Id) && a.IsActive).ToListAsync(ct);

    public Task<ROLE_PASSWORD_POLICY?> GetPolicyLinkAsync(int roleId, CancellationToken ct) =>
        _db.ROLE_PASSWORD_POLICY.FirstOrDefaultAsync(l => l.RoleId == roleId, ct);

    public Task<bool> PasswordPolicyExistsAsync(int passwordPolicyId, CancellationToken ct) =>
        _db.PASSWORD_POLICY.AnyAsync(p => p.Id == passwordPolicyId && p.Status, ct);

    public async Task AddAsync(ROLES role, CancellationToken ct) => await _db.ROLES.AddAsync(role, ct);

    public async Task AddPolicyLinkAsync(ROLE_PASSWORD_POLICY link, CancellationToken ct) => await _db.ROLE_PASSWORD_POLICY.AddAsync(link, ct);

    public async Task RemoveAsync(ROLES role, CancellationToken ct)
    {
        // ROLE_RIGHTS cascade in the database; the policy link does not, so it goes first.
        _db.ROLE_PASSWORD_POLICY.RemoveRange(await _db.ROLE_PASSWORD_POLICY.Where(l => l.RoleId == role.Id).ToListAsync(ct));
        _db.ROLE_RIGHTS.RemoveRange(await _db.ROLE_RIGHTS.Where(r => r.RoleId == role.Id).ToListAsync(ct));
        _db.ROLES.Remove(role);
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
            throw new BusinessException(ErrorCodes.RoleNameTaken, "A role with this name already exists.");
        }
    }
}
