namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>SUPPLIER_CODE</c>: one supplier code of a company, e.g. Globus · RJ · 550 · CL.
/// Roles are assigned per supplier code, and the user works inside one supplier code at a time.
/// The code is unique inside one excise (enforced by the unique key UQ_SUPPLIER_CODE_EXCISE).
/// </summary>
public class SUPPLIER_CODE
{
    private SUPPLIER_CODE()
    {
        SupplierCode = string.Empty;
    }

    public int Id { get; private set; }
    public int CompanyId { get; private set; }            // the customer that owns the supplierCode
    public string? FranchiseName { get; private set; }    // text only: bottling for another brand owner
    public int ExciseId { get; private set; }             // EXCISE.EXCISE_ID (RJ, UP ...)
    public string SupplierCode { get; private set; }      // the code given by the excise department: 550
    public int LiquorCategoryId { get; private set; }     // CL / FL / IMFL
    public bool IsActive { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static SUPPLIER_CODE Create(
        int companyId, string? franchiseName, int exciseId, string supplierCode, int liquorCategoryId, DateTime now, int? createdBy)
    {
        var entity = new SUPPLIER_CODE { CompanyId = companyId, IsActive = true, CreatedAt = now, CreatedBy = createdBy };
        entity.Update(franchiseName, exciseId, supplierCode, liquorCategoryId, now, createdBy);
        return entity;
    }

    /// <summary>The owning company never changes: a supplier code is not moved to another customer.</summary>
    public void Update(string? franchiseName, int exciseId, string supplierCode, int liquorCategoryId, DateTime now, int? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            throw new ArgumentException("Supplier code is required.", nameof(supplierCode));
        }

        FranchiseName = string.IsNullOrWhiteSpace(franchiseName) ? null : franchiseName.Trim();
        ExciseId = exciseId;
        SupplierCode = supplierCode.Trim();
        LiquorCategoryId = liquorCategoryId;
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
