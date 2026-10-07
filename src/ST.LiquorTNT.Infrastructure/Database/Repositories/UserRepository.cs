using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Business.Users;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<USERS?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.USERS.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<USERS?> GetByUserNameAsync(string userName, CancellationToken ct) =>
        _db.USERS.FirstOrDefaultAsync(u => u.UserName == userName, ct);

    public Task<bool> UserNameExistsAsync(string userName, CancellationToken ct) =>
        _db.USERS.AnyAsync(u => u.UserName == userName, ct);

    public async Task<PagedResponse<UserResponse>> GetPageAsync(int? companyId, string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.USERS.AsNoTracking().Where(u => companyId == null || u.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.UserName.Contains(search) || (u.FullName != null && u.FullName.Contains(search)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(UserProjections.ToResponse)
            .ToListAsync(ct);

        return new PagedResponse<UserResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<IReadOnlyList<string>> GetRecentPasswordHashesAsync(int userId, int count, CancellationToken ct)
    {
        if (count <= 0)
        {
            return Array.Empty<string>();
        }

        return await _db.USER_PASSWORD_HISTORY
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .ThenByDescending(h => h.Id)
            .Take(count)
            .Select(h => h.PasswordHash)
            .ToListAsync(ct);
    }

    public async Task AddAsync(USERS user, CancellationToken ct) => await _db.USERS.AddAsync(user, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // Two requests passed the name check at the same moment; the unique index is the final arbiter.
            throw new BusinessException(ErrorCodes.UserNameTaken, "User name is already in use.");
        }
    }
}
