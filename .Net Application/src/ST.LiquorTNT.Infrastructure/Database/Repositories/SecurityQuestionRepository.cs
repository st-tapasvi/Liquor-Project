using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class SecurityQuestionRepository : ISecurityQuestionRepository
{
    private readonly AppDbContext _db;

    public SecurityQuestionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SECURITY_QUESTION>> GetActiveQuestionsAsync(CancellationToken ct) =>
        await _db.SECURITY_QUESTION.AsNoTracking().Where(q => q.Status).OrderBy(q => q.Id).ToListAsync(ct);

    public Task<SECURITY_QUESTION?> GetQuestionAsync(int questionId, CancellationToken ct) =>
        _db.SECURITY_QUESTION.AsNoTracking().FirstOrDefaultAsync(q => q.Id == questionId, ct);

    public Task<USER_SECURITY_QUESTION?> GetActiveForUserAsync(int userId, CancellationToken ct) =>
        _db.USER_SECURITY_QUESTION.FirstOrDefaultAsync(q => q.UserId == userId && q.IsActive, ct);

    public Task<USER_SECURITY_QUESTION?> GetForUserAndQuestionAsync(int userId, int questionId, CancellationToken ct) =>
        _db.USER_SECURITY_QUESTION.FirstOrDefaultAsync(q => q.UserId == userId && q.QuestionId == questionId, ct);

    public async Task AddUserQuestionAsync(USER_SECURITY_QUESTION question, CancellationToken ct) =>
        await _db.USER_SECURITY_QUESTION.AddAsync(question, ct);

    public async Task<IReadOnlyList<PASSWORD_RESET_REQUEST>> GetOpenResetRequestsAsync(int userId, CancellationToken ct) =>
        await _db.PASSWORD_RESET_REQUEST
            .Where(r => r.UserId == userId
                        && (r.Status == PASSWORD_RESET_REQUEST.StatusPending || r.Status == PASSWORD_RESET_REQUEST.StatusVerified))
            .ToListAsync(ct);

    public Task<PASSWORD_RESET_REQUEST?> GetResetRequestByTokenHashAsync(string tokenHash, CancellationToken ct) =>
        _db.PASSWORD_RESET_REQUEST.FirstOrDefaultAsync(r => r.RequestTokenHash == tokenHash, ct);

    public async Task AddResetRequestAsync(PASSWORD_RESET_REQUEST request, CancellationToken ct) =>
        await _db.PASSWORD_RESET_REQUEST.AddAsync(request, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
