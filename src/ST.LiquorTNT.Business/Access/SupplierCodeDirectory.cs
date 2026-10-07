using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Business.Access;

/// <summary>
/// "Which supplier codes may this user pick?" - shared by login (to fill the picker / auto-select) and by
/// <see cref="AccessService"/> (to check a pick). Super Admin may pick any active supplier code of any company.
/// </summary>
public sealed class SupplierCodeDirectory
{
    private readonly IAccessRepository _access;

    public SupplierCodeDirectory(IAccessRepository access) => _access = access;

    public async Task<IReadOnlyList<SupplierCodeResponse>> ForUserAsync(int userId, CancellationToken ct) =>
        await _access.IsSuperAdminAsync(userId, ct)
            ? await _access.GetAllSupplierCodesAsync(ct)
            : await _access.GetSupplierCodesForUserAsync(userId, ct);
}
