using ST.LiquorTNT.Contracts.SecurityConfig;

namespace ST.LiquorTNT.Business.SecurityConfig;

public interface ISecurityConfigService
{
    Task<IReadOnlyList<SecurityConfigResponse>> GetAllAsync(CancellationToken ct);

    Task<SecurityConfigResponse> UpdateAsync(string key, UpdateSecurityConfigRequest request, CancellationToken ct);
}
