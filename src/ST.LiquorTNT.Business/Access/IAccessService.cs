using ST.LiquorTNT.Contracts.Access;
using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Business.Access;

/// <summary>The caller's own access: which supplier codes they may work in, picking one, and their permission keys.</summary>
public interface IAccessService
{
    Task<IReadOnlyList<SupplierCodeResponse>> GetMySupplierCodesAsync(CancellationToken ct);

    /// <summary>Picks (or switches) the supplier code of the current session. Returns the new permissions straight away.</summary>
    Task<MyPermissionsResponse> SelectSupplierCodeAsync(SelectSupplierCodeRequest request, CancellationToken ct);

    Task<MyPermissionsResponse> GetMyPermissionsAsync(CancellationToken ct);
}
