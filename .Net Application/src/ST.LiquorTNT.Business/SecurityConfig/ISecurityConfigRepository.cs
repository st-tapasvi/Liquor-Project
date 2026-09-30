using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.SecurityConfig;

/// <summary>Admin access to SECURITY_CONFIG rows (reading for the app goes through ISecurityConfigProvider).</summary>
public interface ISecurityConfigRepository
{
    Task<IReadOnlyList<SECURITY_CONFIG>> GetAllAsync(CancellationToken ct);

    Task<SECURITY_CONFIG?> GetByKeyAsync(string key, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
