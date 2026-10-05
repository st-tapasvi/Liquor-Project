using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Infrastructure.Database;

namespace ST.LiquorTNT.Infrastructure.Common;

/// <summary>
/// Reads the active rows of SECURITY_CONFIG and maps them to typed <see cref="SecuritySettings"/>.
/// Scoped: the rows are read fresh per request. A longer-lived cache with invalidation is added
/// later when the admin edit screen (Step 19) lands.
/// </summary>
public sealed class SecurityConfigProvider : ISecurityConfigProvider
{
    private readonly AppDbContext _db;

    public SecurityConfigProvider(AppDbContext db) => _db = db;

    public async Task<SecuritySettings> GetAsync(CancellationToken ct)
    {
        var rows = await _db.SECURITY_CONFIG
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.ConfigKey, c.ConfigValue })
            .ToListAsync(ct);

        var entries = rows.ToDictionary(r => r.ConfigKey, r => r.ConfigValue, StringComparer.OrdinalIgnoreCase);

        return SecuritySettings.FromEntries(entries);
    }
}
