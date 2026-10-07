namespace ST.LiquorTNT.Contracts.Companies;

/// <summary>One company (customer) in the Super Admin's company list and dropdowns.</summary>
public sealed class CompanyResponse
{
    public int Id { get; set; }
    public string? CompanyName { get; set; }
    public string? AliasName { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }

    /// <summary>How many supplier codes the company has (0 = none yet, so it has no default roles yet either).</summary>
    public int SupplierCodeCount { get; set; }
}
