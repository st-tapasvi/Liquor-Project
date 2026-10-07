using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.SupplierCodes;

/// <summary>SUPPLIER_CODE data access. Implemented in Infrastructure.</summary>
public interface ISupplierCodeRepository
{
    Task<SUPPLIER_CODE?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>One supplier code with company, excise and category names, or null.</summary>
    Task<SupplierCodeResponse?> GetResponseAsync(int id, CancellationToken ct);

    /// <summary>Server-paged list. <paramref name="companyId"/> null = every company (Super Admin only).</summary>
    Task<PagedResponse<SupplierCodeResponse>> GetPageAsync(int? companyId, string? search, int page, int pageSize, CancellationToken ct);

    /// <summary>Is this code already used inside this excise (by another supplier code than <paramref name="excludeId"/>)?</summary>
    Task<bool> CodeExistsAsync(int exciseId, string supplierCode, int? excludeId, CancellationToken ct);

    Task<bool> ExciseExistsAsync(int exciseId, CancellationToken ct);

    Task<bool> LiquorCategoryIsActiveAsync(int liquorCategoryId, CancellationToken ct);

    Task AddAsync(SUPPLIER_CODE supplierCode, CancellationToken ct);

    /// <summary>Saves; a duplicate (excise, code) becomes 409 SUPPLIER_CODE_TAKEN.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
