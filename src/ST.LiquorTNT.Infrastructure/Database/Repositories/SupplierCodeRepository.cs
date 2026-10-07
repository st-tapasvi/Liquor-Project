using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.SupplierCodes;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class SupplierCodeRepository : ISupplierCodeRepository
{
    private readonly AppDbContext _db;

    public SupplierCodeRepository(AppDbContext db) => _db = db;

    public Task<SUPPLIER_CODE?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.SUPPLIER_CODE.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<SupplierCodeResponse?> GetResponseAsync(int id, CancellationToken ct) =>
        (await SupplierCodeQuery.Project(_db, _db.SUPPLIER_CODE.Where(s => s.Id == id)).ToListAsync(ct)).Select(SupplierCodeQuery.WithDisplayName).FirstOrDefault();

    public async Task<PagedResponse<SupplierCodeResponse>> GetPageAsync(
        int? companyId, string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = SupplierCodeQuery.Project(_db, _db.SUPPLIER_CODE.Where(s => companyId == null || s.CompanyId == companyId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.SupplierCode.Contains(search)
                                     || (s.FranchiseName != null && s.FranchiseName.Contains(search))
                                     || (s.CompanyName != null && s.CompanyName.Contains(search)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.CompanyName).ThenBy(s => s.ExciseCode).ThenBy(s => s.SupplierCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<SupplierCodeResponse>
        {
            Items = items.Select(SupplierCodeQuery.WithDisplayName).ToList(), Page = page, PageSize = pageSize, TotalCount = total,
        };
    }

    public Task<bool> CodeExistsAsync(int exciseId, string supplierCode, int? excludeId, CancellationToken ct) =>
        _db.SUPPLIER_CODE.AnyAsync(s => s.ExciseId == exciseId && s.SupplierCode == supplierCode && s.Id != excludeId, ct);

    public Task<bool> ExciseExistsAsync(int exciseId, CancellationToken ct) =>
        _db.EXCISE.AnyAsync(e => e.Id == exciseId && e.IsActive, ct);

    public Task<bool> LiquorCategoryIsActiveAsync(int liquorCategoryId, CancellationToken ct) =>
        _db.LIQUOR_CATEGORY.AnyAsync(c => c.Id == liquorCategoryId && c.IsActive, ct);

    public async Task AddAsync(SUPPLIER_CODE supplierCode, CancellationToken ct) => await _db.SUPPLIER_CODE.AddAsync(supplierCode, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new BusinessException(ErrorCodes.SupplierCodeTaken, "This supplier code already exists in this excise.");
        }
    }
}
