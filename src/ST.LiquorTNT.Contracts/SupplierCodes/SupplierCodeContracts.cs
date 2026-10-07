namespace ST.LiquorTNT.Contracts.SupplierCodes;

public sealed class SupplierCodeResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? FranchiseName { get; set; }
    public int ExciseId { get; set; }
    public string ExciseCode { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public int LiquorCategoryId { get; set; }
    public string LiquorCategoryCode { get; set; } = string.Empty;

    /// <summary>"RJ CL 550"</summary>
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class CreateSupplierCodeRequest
{
    /// <summary>The customer the supplier code belongs to (Super Admin creates supplier codes for any company).</summary>
    public int CompanyId { get; set; }
    public string? FranchiseName { get; set; }
    public int ExciseId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public int LiquorCategoryId { get; set; }
}

/// <summary>The company of a supplier code never changes, so it is not part of an edit.</summary>
public sealed class UpdateSupplierCodeRequest
{
    public string? FranchiseName { get; set; }
    public int ExciseId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public int LiquorCategoryId { get; set; }
}

public sealed class SupplierCodeListRequest
{
    /// <summary>Matches the supplier code, franchise or company name (contains).</summary>
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
