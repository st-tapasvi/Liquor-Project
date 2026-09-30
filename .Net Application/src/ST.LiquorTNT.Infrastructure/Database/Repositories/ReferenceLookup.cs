using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Users;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class ReferenceLookup : IReferenceLookup
{
    private readonly AppDbContext _db;

    public ReferenceLookup(AppDbContext db) => _db = db;

    public Task<bool> RoleExistsAsync(int roleId, CancellationToken ct) =>
        _db.ROLES.AnyAsync(r => r.Id == roleId && r.IsActive, ct);

    public Task<bool> CompanyExistsAsync(int companyId, CancellationToken ct) =>
        _db.COMPANY.AnyAsync(c => c.Id == companyId && c.IsActive, ct);
}
