namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>LIQUOR_CATEGORY</c>: CL, FL, IMFL ... A supplier code (SUPPLIER_CODE) belongs to one category.
/// The real list is maintained from the CRM; a category is deactivated, never deleted, because supplier codes point to it.
/// </summary>
public class LIQUOR_CATEGORY
{
    private LIQUOR_CATEGORY()
    {
        CategoryCode = string.Empty;
        CategoryName = string.Empty;
    }

    public int Id { get; private set; }
    public string CategoryCode { get; private set; }      // short code shown everywhere: IMFL
    public string CategoryName { get; private set; }      // full name: Indian Made Foreign Liquor
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static LIQUOR_CATEGORY Create(string categoryCode, string categoryName, string? description, DateTime now, int? createdBy)
    {
        var category = new LIQUOR_CATEGORY { IsActive = true, CreatedAt = now, CreatedBy = createdBy };
        category.Update(categoryCode, categoryName, description, now, createdBy);
        return category;
    }

    public void Update(string categoryCode, string categoryName, string? description, DateTime now, int? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(categoryCode) || string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category code and name are required.");
        }

        // Codes are compared and shown in capitals (IMFL, CL), whatever the user typed.
        CategoryCode = categoryCode.Trim().ToUpperInvariant();
        CategoryName = categoryName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SetActive(bool isActive, DateTime now, int? updatedBy)
    {
        IsActive = isActive;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }
}
