using ST.LiquorTNT.Contracts.LiquorCategories;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.LiquorCategories;

/// <summary>LIQUOR_CATEGORY data access. Implemented in Infrastructure.</summary>
public interface ILiquorCategoryRepository
{
    Task<LIQUOR_CATEGORY?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>All categories (active and inactive), by code.</summary>
    Task<IReadOnlyList<LiquorCategoryResponse>> GetAllAsync(CancellationToken ct);

    Task<bool> CodeExistsAsync(string categoryCode, int? excludeId, CancellationToken ct);

    Task AddAsync(LIQUOR_CATEGORY category, CancellationToken ct);

    /// <summary>Saves; a duplicate code (unique key) becomes 409 LIQUOR_CATEGORY_CODE_TAKEN.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
