using FluentValidation;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.LiquorCategories;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.LiquorCategories;

/// <summary>
/// The liquor category master (CL / FL / IMFL ...). Everyone with liquorcategory.view may read it; only Super Admin
/// changes it (the add / edit rights are SYSTEM scope, so no company role can hold them). A category is deactivated,
/// never deleted, because supplier codes point to it.
/// </summary>
public sealed class LiquorCategoryService : ILiquorCategoryService
{
    private const string EntityName = "LIQUOR_CATEGORY";

    private readonly ILiquorCategoryRepository _categories;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IUserLogWriter _log;
    private readonly IValidator<SaveLiquorCategoryRequest> _validator;

    public LiquorCategoryService(
        ILiquorCategoryRepository categories,
        ICurrentUser user,
        IClock clock,
        IUserLogWriter log,
        IValidator<SaveLiquorCategoryRequest> validator)
    {
        _categories = categories;
        _user = user;
        _clock = clock;
        _log = log;
        _validator = validator;
    }

    public Task<IReadOnlyList<LiquorCategoryResponse>> GetAllAsync(CancellationToken ct) => _categories.GetAllAsync(ct);

    public async Task<LiquorCategoryResponse> GetByIdAsync(int id, CancellationToken ct) => Map(await RequireAsync(id, ct));

    public async Task<LiquorCategoryResponse> CreateAsync(SaveLiquorCategoryRequest request, CancellationToken ct)
    {
        (await _validator.ValidateAsync(request, ct)).EnsureValid();
        await EnsureCodeFreeAsync(request.CategoryCode, excludeId: null, ct);

        var category = LIQUOR_CATEGORY.Create(request.CategoryCode, request.CategoryName, request.Description, _clock.IndiaNow, _user.UserId);

        // The audit row needs the generated Id (same two-commit pattern as user create).
        await _categories.AddAsync(category, ct);
        await _categories.SaveChangesAsync(ct);

        var response = Map(category);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.MasterCreated, UserLogModules.Masters, EntityName,
            category.Id.ToString(), $"Liquor category '{category.CategoryCode}' created.", newValue: response), ct);
        await _categories.SaveChangesAsync(ct);

        return response;
    }

    public async Task<LiquorCategoryResponse> UpdateAsync(int id, SaveLiquorCategoryRequest request, CancellationToken ct)
    {
        (await _validator.ValidateAsync(request, ct)).EnsureValid();

        var category = await RequireAsync(id, ct);
        await EnsureCodeFreeAsync(request.CategoryCode, category.Id, ct);

        var before = Map(category);
        category.Update(request.CategoryCode, request.CategoryName, request.Description, _clock.IndiaNow, _user.UserId);

        var after = Map(category);
        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.MasterUpdated, UserLogModules.Masters, EntityName,
            category.Id.ToString(), $"Liquor category '{category.CategoryCode}' updated.", oldValue: before, newValue: after), ct);
        await _categories.SaveChangesAsync(ct);

        return after;
    }

    public async Task<LiquorCategoryResponse> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var category = await RequireAsync(id, ct);
        category.SetActive(isActive, _clock.IndiaNow, _user.UserId);

        await _log.WriteAsync(UserLogEntry.Success(UserLogActions.MasterUpdated, UserLogModules.Masters, EntityName,
            category.Id.ToString(), $"Liquor category '{category.CategoryCode}' {(isActive ? "activated" : "deactivated")}."), ct);
        await _categories.SaveChangesAsync(ct);

        return Map(category);
    }

    private async Task<LIQUOR_CATEGORY> RequireAsync(int id, CancellationToken ct) =>
        await _categories.GetByIdAsync(id, ct) ?? throw new NotFoundException("Liquor category");

    private async Task EnsureCodeFreeAsync(string code, int? excludeId, CancellationToken ct)
    {
        if (await _categories.CodeExistsAsync(code.Trim().ToUpperInvariant(), excludeId, ct))
        {
            throw new BusinessException(ErrorCodes.LiquorCategoryCodeTaken, "This category code already exists.",
                $"Choose another code than '{code.Trim().ToUpperInvariant()}'.");
        }
    }

    private static LiquorCategoryResponse Map(LIQUOR_CATEGORY c) => new()
    {
        Id = c.Id,
        CategoryCode = c.CategoryCode,
        CategoryName = c.CategoryName,
        Description = c.Description,
        IsActive = c.IsActive,
    };
}
