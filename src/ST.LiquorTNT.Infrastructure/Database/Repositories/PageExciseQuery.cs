using Microsoft.EntityFrameworkCore;

namespace ST.LiquorTNT.Infrastructure.Database.Repositories;

/// <summary>
/// The excise codes of pages and actions, read from PAGE_EXCISES / PAGE_ACTION_EXCISES joined to EXCISE. A page or action
/// missing from its map has no rows = every excise (null in the API). Pages and actions are a few dozen rows, so both maps
/// are read whole; the shared rule <c>Domain.Rules.ExciseScope</c> is then applied in memory.
/// </summary>
internal static class PageExciseQuery
{
    /// <summary>Page id → excise codes ("JK", "RJ"), only for pages limited to some excises.</summary>
    public static async Task<Dictionary<int, IReadOnlyList<string>>> PageCodesAsync(AppDbContext db, CancellationToken ct) =>
        (await (from pe in db.PAGE_EXCISES.AsNoTracking()
                join e in db.EXCISE on pe.ExciseId equals e.Id
                select new { pe.PageId, e.ExciseCode })
            .ToListAsync(ct))
        .GroupBy(r => r.PageId)
        .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.ExciseCode).OrderBy(c => c).ToList());

    /// <summary>Page action id → excise codes, only for actions limited to some excises.</summary>
    public static async Task<Dictionary<int, IReadOnlyList<string>>> ActionCodesAsync(AppDbContext db, CancellationToken ct) =>
        (await (from ae in db.PAGE_ACTION_EXCISES.AsNoTracking()
                join e in db.EXCISE on ae.ExciseId equals e.Id
                select new { ae.PageActionId, e.ExciseCode })
            .ToListAsync(ct))
        .GroupBy(r => r.PageActionId)
        .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.ExciseCode).OrderBy(c => c).ToList());
}
