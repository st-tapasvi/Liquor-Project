namespace ST.LiquorTNT.Contracts.LiquorCategories;

public sealed class LiquorCategoryResponse
{
    public int Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Body for creating and for editing a category (same fields).</summary>
public sealed class SaveLiquorCategoryRequest
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
