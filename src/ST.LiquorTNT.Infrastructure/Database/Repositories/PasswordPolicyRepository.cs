using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class PasswordPolicyRepository : IPasswordPolicyRepository
{
    private readonly AppDbContext _db;

    public PasswordPolicyRepository(AppDbContext db) => _db = db;

    public Task<PASSWORD_POLICY?> GetForRoleAsync(int roleId, CancellationToken ct) =>
        (from map in _db.ROLE_PASSWORD_POLICY
         join policy in _db.PASSWORD_POLICY on map.PasswordPolicyId equals policy.Id
         where map.RoleId == roleId && policy.Status
         select policy)
        .AsNoTracking()
        .FirstOrDefaultAsync(ct);

    public Task<PASSWORD_POLICY?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PASSWORD_POLICY.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PASSWORD_POLICY>> GetAllAsync(CancellationToken ct) =>
        await _db.PASSWORD_POLICY.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
