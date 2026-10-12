using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Business.Access;

/// <summary>
/// Reads who may do what. Declared here because the Access feature owns it; implemented in Infrastructure.
/// "Holds a supplier code" means: has a role or a custom right on that supplier code, or on all supplier codes of the company
/// (SUPPLIER_CODE_ID null).
/// </summary>
public interface IAccessRepository
{
    /// <summary>True when the user holds the Super Admin role (ROLES.IS_SYSTEM).</summary>
    Task<bool> IsSuperAdminAsync(int userId, CancellationToken ct);

    /// <summary>
    /// The user's permission keys inside one supplier code: rights of the user's roles (direct or through role groups) of
    /// that supplier code or company-level, plus custom rights for that supplier code (or for all supplier codes).
    /// Distinct keys of active actions that exist in the supplier code's excise (PAGE_EXCISES / PAGE_ACTION_EXCISES).
    /// </summary>
    Task<IReadOnlyList<string>> GetPermissionKeysAsync(int userId, int supplierCodeId, CancellationToken ct);

    /// <summary>Every active permission key (what Admin holds); with an excise code only the keys that exist in that excise.</summary>
    Task<IReadOnlyList<string>> GetAllPermissionKeysAsync(string? exciseCode, CancellationToken ct);

    /// <summary>The active supplier codes the user holds, ordered for the picker.</summary>
    Task<IReadOnlyList<SupplierCodeResponse>> GetSupplierCodesForUserAsync(int userId, CancellationToken ct);

    /// <summary>Every active supplier code of every company (what Super Admin may pick).</summary>
    Task<IReadOnlyList<SupplierCodeResponse>> GetAllSupplierCodesAsync(CancellationToken ct);

    /// <summary>One ACTIVE supplier code, or null when it does not exist or was deactivated.</summary>
    Task<SupplierCodeResponse?> GetSupplierCodeAsync(int supplierCodeId, CancellationToken ct);
}
