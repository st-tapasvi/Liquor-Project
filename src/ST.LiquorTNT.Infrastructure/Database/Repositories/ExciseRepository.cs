using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Excises;
using ST.LiquorTNT.Contracts.Excises;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

public sealed class ExciseRepository : IExciseRepository
{
    private readonly AppDbContext _db;

    public ExciseRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ExciseResponse>> GetAllAsync(CancellationToken ct) =>
        await _db.EXCISE.AsNoTracking()
            .OrderBy(e => e.ExciseCode)
            .Select(e => new ExciseResponse { Id = e.Id, ExciseCode = e.ExciseCode, ExciseName = e.ExciseName, IsActive = e.IsActive })
            .ToListAsync(ct);
}
