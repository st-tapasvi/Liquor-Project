using ST.LiquorTNT.Contracts.LiquorCategories;

namespace ST.LiquorTNT.Business.LiquorCategories;

public interface ILiquorCategoryService
{
    Task<IReadOnlyList<LiquorCategoryResponse>> GetAllAsync(CancellationToken ct);

    Task<LiquorCategoryResponse> GetByIdAsync(int id, CancellationToken ct);

    Task<LiquorCategoryResponse> CreateAsync(SaveLiquorCategoryRequest request, CancellationToken ct);

    Task<LiquorCategoryResponse> UpdateAsync(int id, SaveLiquorCategoryRequest request, CancellationToken ct);

    Task<LiquorCategoryResponse> SetActiveAsync(int id, bool isActive, CancellationToken ct);
}
