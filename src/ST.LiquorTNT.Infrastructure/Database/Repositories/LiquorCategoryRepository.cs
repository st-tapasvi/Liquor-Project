using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.LiquorCategories;
using ST.LiquorTNT.Contracts.LiquorCategories;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class LiquorCategoryRepository : ILiquorCategoryRepository
{
    private readonly AppDbContext _db;

    public LiquorCategoryRepository(AppDbContext db) => _db = db;

    public Task<LIQUOR_CATEGORY?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.LIQUOR_CATEGORY.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<LiquorCategoryResponse>> GetAllAsync(CancellationToken ct) =>
        await _db.LIQUOR_CATEGORY.AsNoTracking()
            .OrderBy(c => c.CategoryCode)
            .Select(c => new LiquorCategoryResponse
            {
                Id = c.Id,
                CategoryCode = c.CategoryCode,
                CategoryName = c.CategoryName,
                Description = c.Description,
                IsActive = c.IsActive,
            })
            .ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string categoryCode, int? excludeId, CancellationToken ct) =>
        _db.LIQUOR_CATEGORY.AnyAsync(c => c.CategoryCode == categoryCode && c.Id != excludeId, ct);

    public async Task AddAsync(LIQUOR_CATEGORY category, CancellationToken ct) => await _db.LIQUOR_CATEGORY.AddAsync(category, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new BusinessException(ErrorCodes.LiquorCategoryCodeTaken, "This category code already exists.");
        }
    }
}
