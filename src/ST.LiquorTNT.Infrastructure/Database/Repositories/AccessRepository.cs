using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

/// <summary>
/// Reads effective rights. A role works in its own supplier code (ROLES.SUPPLIER_CODE_ID); a company-level role
/// (SUPPLIER_CODE_ID null) and a custom right with SUPPLIER_CODE_ID null count for every supplier code of the user's
/// OWN company only - the company check is repeated here, so a bad row can never leak rights into another
/// company's supplier code.
/// </summary>
public sealed class AccessRepository : IAccessRepository
{
    private readonly AppDbContext _db;

    public AccessRepository(AppDbContext db) => _db = db;

    public Task<bool> IsSuperAdminAsync(int userId, CancellationToken ct) =>
        (from ur in _db.USER_ROLES
         join r in _db.ROLES on ur.RoleId equals r.Id
         where ur.UserId == userId && r.IsSystem && r.IsActive
         select ur.Id)
        .AnyAsync(ct);

    public async Task<IReadOnlyList<string>> GetPermissionKeysAsync(int userId, int supplierCodeId, CancellationToken ct)
    {
        // "All supplier codes" rows apply only when the supplier code belongs to the user's own company.
        var supplierCodeCompany = await _db.SUPPLIER_CODE.Where(s => s.Id == supplierCodeId).Select(s => (int?)s.CompanyId).FirstOrDefaultAsync(ct);
        var userCompany = await _db.USERS.Where(u => u.Id == userId).Select(u => u.CompanyId).FirstOrDefaultAsync(ct);
        var allSupplierCodesCount = supplierCodeCompany is not null && supplierCodeCompany == userCompany;

        // rights through roles
        var fromRoles =
            from ur in _db.USER_ROLES
            join r in _db.ROLES on ur.RoleId equals r.Id
            join rr in _db.ROLE_RIGHTS on r.Id equals rr.RoleId
            join pa in _db.PAGE_ACTIONS on rr.PageActionId equals pa.Id
            where ur.UserId == userId
                  && r.IsActive && pa.IsActive
                  && (r.SupplierCodeId == supplierCodeId || (allSupplierCodesCount && r.SupplierCodeId == null))
            select pa.PermissionKey;

        // custom rights
        var fromUser =
            from uri in _db.USER_RIGHTS
            join pa in _db.PAGE_ACTIONS on uri.PageActionId equals pa.Id
            where uri.UserId == userId
                  && pa.IsActive
                  && (uri.SupplierCodeId == supplierCodeId || (allSupplierCodesCount && uri.SupplierCodeId == null))
            select pa.PermissionKey;

        return await fromRoles.Union(fromUser).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetAllPermissionKeysAsync(CancellationToken ct) =>
        await _db.PAGE_ACTIONS.AsNoTracking().Where(a => a.IsActive).Select(a => a.PermissionKey).ToListAsync(ct);

    public async Task<IReadOnlyList<SupplierCodeResponse>> GetSupplierCodesForUserAsync(int userId, CancellationToken ct)
    {
        var userCompany = await _db.USERS.Where(u => u.Id == userId).Select(u => u.CompanyId).FirstOrDefaultAsync(ct);

        // Supplier codes of the user's roles and custom rights ...
        var namedByRole =
            from ur in _db.USER_ROLES
            join r in _db.ROLES on ur.RoleId equals r.Id
            where ur.UserId == userId && r.IsActive && r.SupplierCodeId != null
            select r.SupplierCodeId!.Value;
        var namedByRight = _db.USER_RIGHTS.Where(r => r.UserId == userId && r.SupplierCodeId != null).Select(r => r.SupplierCodeId!.Value);

        // ... plus every supplier code of the company when some role is company-level or some right is for "all supplier codes".
        var coversAll =
            await (from ur in _db.USER_ROLES
                   join r in _db.ROLES on ur.RoleId equals r.Id
                   where ur.UserId == userId && r.SupplierCodeId == null && r.IsActive && !r.IsSystem
                   select ur.Id).AnyAsync(ct)
            || await _db.USER_RIGHTS.AnyAsync(r => r.UserId == userId && r.SupplierCodeId == null, ct);

        var supplierCodes = _db.SUPPLIER_CODE.Where(s =>
            s.IsActive
            && s.CompanyId == userCompany
            && (coversAll || namedByRole.Contains(s.Id) || namedByRight.Contains(s.Id)));

        return await SupplierCodeQuery.ToResponsesAsync(_db, supplierCodes, ct);
    }

    public async Task<IReadOnlyList<SupplierCodeResponse>> GetAllSupplierCodesAsync(CancellationToken ct) =>
        await SupplierCodeQuery.ToResponsesAsync(_db, _db.SUPPLIER_CODE.Where(s => s.IsActive), ct);

    public async Task<SupplierCodeResponse?> GetSupplierCodeAsync(int supplierCodeId, CancellationToken ct) =>
        (await SupplierCodeQuery.ToResponsesAsync(_db, _db.SUPPLIER_CODE.Where(s => s.Id == supplierCodeId && s.IsActive), ct)).FirstOrDefault();
}
