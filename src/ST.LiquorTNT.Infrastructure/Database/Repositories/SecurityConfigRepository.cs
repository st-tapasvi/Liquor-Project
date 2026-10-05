using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.SecurityConfig;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class SecurityConfigRepository : ISecurityConfigRepository
{
    private readonly AppDbContext _db;

    public SecurityConfigRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SECURITY_CONFIG>> GetAllAsync(CancellationToken ct) =>
        await _db.SECURITY_CONFIG.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Id).ToListAsync(ct);

    public Task<SECURITY_CONFIG?> GetByKeyAsync(string key, CancellationToken ct) =>
        _db.SECURITY_CONFIG.FirstOrDefaultAsync(c => c.ConfigKey == key && c.IsActive, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
