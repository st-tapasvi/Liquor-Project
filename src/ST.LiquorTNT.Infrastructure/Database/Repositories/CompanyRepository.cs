using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Companies;
using ST.LiquorTNT.Contracts.Companies;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;

    public CompanyRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CompanyResponse>> GetListAsync(int? companyId, CancellationToken ct) =>
        await _db.COMPANY.AsNoTracking()
            .Where(c => companyId == null || c.Id == companyId)
            .OrderBy(c => c.CompanyName)
            .Select(c => new CompanyResponse
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                AliasName = c.AliasName,
                City = c.City,
                IsActive = c.IsActive,
                SupplierCodeCount = _db.SUPPLIER_CODE.Count(s => s.CompanyId == c.Id),
            })
            .ToListAsync(ct);
}
