using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class PasswordPolicyRepository : IPasswordPolicyRepository
{
    private readonly AppDbContext _db;

    public PasswordPolicyRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<int, PASSWORD_POLICY>> GetForRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken ct)
    {
        var rows = await (from map in _db.ROLE_PASSWORD_POLICY
                          join policy in _db.PASSWORD_POLICY on map.PasswordPolicyId equals policy.Id
                          where roleIds.Contains(map.RoleId) && policy.Status
                          select new { map.RoleId, Policy = policy })
                         .AsNoTracking()
                         .ToListAsync(ct);

        return rows.ToDictionary(r => r.RoleId, r => r.Policy);
    }

    // direct roles and the roles of the user's role groups: the strictest policy of all of them applies
    public async Task<IReadOnlyList<int>> GetRoleIdsForUserAsync(int userId, CancellationToken ct) =>
        await UserRoleQuery.RoleIds(_db, userId).ToListAsync(ct);

    public Task<PASSWORD_POLICY?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PASSWORD_POLICY.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PASSWORD_POLICY>> GetAllAsync(CancellationToken ct) =>
        await _db.PASSWORD_POLICY.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
