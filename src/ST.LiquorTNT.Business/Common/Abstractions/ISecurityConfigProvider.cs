namespace ST.LiquorTNT.Business.Common.Abstractions;

/// <summary>
/// Reads the global SECURITY_CONFIG table and returns it as typed <see cref="SecuritySettings"/>.
/// Used by Auth, the Users module, sessions and password rules, so it is cross-cutting — hence it
/// sits in Common/Abstractions. Declared in Business, implemented in Infrastructure.
/// </summary>
public interface ISecurityConfigProvider
{
    Task<SecuritySettings> GetAsync(CancellationToken ct);
}
