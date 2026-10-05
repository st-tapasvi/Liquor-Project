using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly AppDbContext _db;

    public SessionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(USER_SESSION session, CancellationToken ct) => await _db.USER_SESSION.AddAsync(session, ct);

    public Task<USER_SESSION?> GetByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        _db.USER_SESSION.FirstOrDefaultAsync(s => s.SessionTokenHash == tokenHash, ct);

    public Task<USER_SESSION?> GetByIdForUserAsync(int sessionId, int userId, CancellationToken ct) =>
        _db.USER_SESSION.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);

    public async Task<IReadOnlyList<USER_SESSION>> GetActiveForUserAsync(int userId, DateTime now, CancellationToken ct) =>
        await Active(userId, now).OrderByDescending(s => s.LoginAt).ToListAsync(ct);

    public Task<int> CountActiveAsync(int userId, DateTime now, CancellationToken ct) =>
        Active(userId, now).CountAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    private IQueryable<USER_SESSION> Active(int userId, DateTime now) =>
        _db.USER_SESSION.Where(s => s.UserId == userId && s.Status == USER_SESSION.StatusActive && s.ExpiresAt > now);
}
