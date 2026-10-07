using Microsoft.AspNetCore.Mvc;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Access;
using ST.LiquorTNT.Business.LiquorCategories;
using ST.LiquorTNT.Contracts.LiquorCategories;

namespace ST.LiquorTNT.Api.Controllers;

/// <summary>Liquor categories (CL / FL / IMFL ...). Changing them is Super Admin only (SYSTEM-scope rights).</summary>
[ApiController]
[Route("api/liquorcategories")]
public sealed class LiquorCategoriesController : ControllerBase
{
    private readonly ILiquorCategoryService _categories;

    public LiquorCategoriesController(ILiquorCategoryService categories) => _categories = categories;

    [HttpGet]
    [HasPermission(Permissions.LiquorCategoryView)]
    public async Task<ActionResult<IReadOnlyList<LiquorCategoryResponse>>> GetAllAsync(CancellationToken ct)
        => Ok(await _categories.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.LiquorCategoryView)]
    public async Task<ActionResult<LiquorCategoryResponse>> GetByIdAsync(int id, CancellationToken ct)
        => Ok(await _categories.GetByIdAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.LiquorCategoryAdd)]
    public async Task<ActionResult<LiquorCategoryResponse>> CreateAsync(SaveLiquorCategoryRequest request, CancellationToken ct)
    {
        var created = await _categories.CreateAsync(request, ct);
        return Created($"/api/liquorcategories/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.LiquorCategoryEdit)]
    public async Task<ActionResult<LiquorCategoryResponse>> UpdateAsync(int id, SaveLiquorCategoryRequest request, CancellationToken ct)
        => Ok(await _categories.UpdateAsync(id, request, ct));

    [HttpPost("{id:int}/activate")]
    [HasPermission(Permissions.LiquorCategoryEdit)]
    public async Task<ActionResult<LiquorCategoryResponse>> ActivateAsync(int id, CancellationToken ct)
        => Ok(await _categories.SetActiveAsync(id, isActive: true, ct));

    [HttpPost("{id:int}/deactivate")]
    [HasPermission(Permissions.LiquorCategoryEdit)]
    public async Task<ActionResult<LiquorCategoryResponse>> DeactivateAsync(int id, CancellationToken ct)
        => Ok(await _categories.SetActiveAsync(id, isActive: false, ct));
}
