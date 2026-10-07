using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

/// <summary>
/// The one definition of "a supplier code as the screen shows it": SUPPLIER_CODE joined to its company, excise and
/// liquor category, with the display name "RJ CL 550" (+ franchise). Used by the access, user-access and
/// supplier-code repositories, so the picker, the access screen and the master list always show the same thing.
/// </summary>
internal static class SupplierCodeQuery
{
    /// <summary>Supplier code + names, still a database query (callers may filter, sort and page it).</summary>
    public static IQueryable<SupplierCodeResponse> Project(AppDbContext db, IQueryable<SUPPLIER_CODE> source) =>
        from s in source.AsNoTracking()
        join e in db.EXCISE on s.ExciseId equals e.Id
        join c in db.LIQUOR_CATEGORY on s.LiquorCategoryId equals c.Id
        join co in db.COMPANY on s.CompanyId equals co.Id
        select new SupplierCodeResponse
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            CompanyName = co.CompanyName,
            FranchiseName = s.FranchiseName,
            ExciseId = s.ExciseId,
            ExciseCode = e.ExciseCode,
            SupplierCode = s.SupplierCode,
            LiquorCategoryId = s.LiquorCategoryId,
            LiquorCategoryCode = c.CategoryCode,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
        };

    /// <summary>Supplier codes of <paramref name="source"/> with names, ordered for a picker (excise, category, code).</summary>
    public static async Task<List<SupplierCodeResponse>> ToResponsesAsync(AppDbContext db, IQueryable<SUPPLIER_CODE> source, CancellationToken ct)
    {
        var rows = await Project(db, source)
            .OrderBy(s => s.ExciseCode).ThenBy(s => s.LiquorCategoryCode).ThenBy(s => s.SupplierCode)
            .ToListAsync(ct);

        return rows.Select(WithDisplayName).ToList();
    }

    /// <summary>Fills <see cref="SupplierCodeResponse.DisplayName"/> after the rows are read.</summary>
    public static SupplierCodeResponse WithDisplayName(SupplierCodeResponse s)
    {
        s.DisplayName = DisplayName(s.ExciseCode, s.LiquorCategoryCode, s.SupplierCode, s.FranchiseName);
        return s;
    }

    /// <summary>"RJ CL 550", or "RJ CL 550 (Franchise)" when the supplier code bottles for a franchise.</summary>
    public static string DisplayName(string exciseCode, string categoryCode, string supplierCode, string? franchiseName) =>
        string.IsNullOrWhiteSpace(franchiseName)
            ? $"{exciseCode} {categoryCode} {supplierCode}"
            : $"{exciseCode} {categoryCode} {supplierCode} ({franchiseName})";
}
