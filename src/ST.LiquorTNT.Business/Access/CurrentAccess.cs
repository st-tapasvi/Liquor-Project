using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;

namespace ST.LiquorTNT.Business.Access;

/// <summary>
/// "What may the caller do right now?" - the one place that answers it, for the [HasPermission] check in the
/// API and for the rules inside services (for example "only a holder of user.manageadmin may edit an admin user").
/// <para>
/// Rules: Super Admin may do everything. Everyone else needs a supplier code picked for the session, and then has
/// exactly the rights of their roles + custom rights for that supplier code (see <see cref="IAccessRepository"/>).
/// </para>
/// Registered per request (scoped): the database is asked once per request and the answer is kept, so several
/// checks in one call cost one query. A change made by an administrator therefore applies from the next call.
/// </summary>
public sealed class CurrentAccess
{
    private readonly IAccessRepository _access;
    private readonly ICurrentUser _user;
    private readonly ITenantContext _tenant;

    private bool? _isSuperAdmin;
    private HashSet<string>? _permissions;

    public CurrentAccess(IAccessRepository access, ICurrentUser user, ITenantContext tenant)
    {
        _access = access;
        _user = user;
        _tenant = tenant;
    }

    /// <summary>The logged-in user's id. Every caller of this class is authenticated, so a missing id is a broken session.</summary>
    public int UserId => _user.UserId
                         ?? throw new UnauthorizedException(ErrorCodes.SessionInvalid, "Not authenticated.");

    public async Task<bool> IsSuperAdminAsync(CancellationToken ct)
    {
        _isSuperAdmin ??= await _access.IsSuperAdminAsync(UserId, ct);
        return _isSuperAdmin.Value;
    }

    /// <summary>
    /// The caller's permission keys in the active supplier code. Super Admin gets every key. Everyone else gets an
    /// empty set until a supplier code is picked.
    /// </summary>
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken ct)
    {
        if (_permissions is null)
        {
            IReadOnlyList<string> keys;

            if (await IsSuperAdminAsync(ct))
            {
                keys = await _access.GetAllPermissionKeysAsync(ct);
            }
            else if (_tenant.SupplierCodeId is int supplierCode)
            {
                keys = await _access.GetPermissionKeysAsync(UserId, supplierCode, ct);
            }
            else
            {
                keys = Array.Empty<string>();
            }

            _permissions = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        }

        return _permissions;
    }

    /// <summary>True when the caller holds <paramref name="permissionKey"/> (Super Admin always does).</summary>
    public async Task<bool> HasPermissionAsync(string permissionKey, CancellationToken ct) =>
        await IsSuperAdminAsync(ct) || (await GetPermissionsAsync(ct)).Contains(permissionKey);

    /// <summary>
    /// Throws unless the caller holds <paramref name="permissionKey"/>:
    /// 409 SUPPLIER_CODE_NOT_SELECTED when no supplier code is picked yet (the screen shows the picker),
    /// 403 PERMISSION_DENIED when the supplier code is picked but the right is missing.
    /// </summary>
    public async Task EnsurePermissionAsync(string permissionKey, CancellationToken ct)
    {
        if (await IsSuperAdminAsync(ct))
        {
            return;
        }

        RequireSupplierCode();

        if (!(await GetPermissionsAsync(ct)).Contains(permissionKey))
        {
            throw new ForbiddenException(ErrorCodes.PermissionDenied,
                "You do not have the right to do this.",
                $"This action needs the permission '{permissionKey}' in the selected supplier code. Ask your administrator to add it to one of your roles.");
        }
    }

    /// <summary>
    /// Throws 403 ADMIN_USER_PROTECTED unless the caller holds user.manageadmin. Guards everything that touches
    /// admin users, admin roles or ADMIN-scope rights, so an Agent Manager cannot reach above their own level.
    /// </summary>
    public async Task EnsureCanManageAdminsAsync(string what, CancellationToken ct)
    {
        if (!await HasPermissionAsync(Permissions.UserManageAdmin, ct))
        {
            throw new ForbiddenException(ErrorCodes.AdminUserProtected,
                "Only an administrator can do this.",
                $"{what} needs the permission '{Permissions.UserManageAdmin}' (Plant Admin has it by default).");
        }
    }

    /// <summary>The active supplier code, or 409 SUPPLIER_CODE_NOT_SELECTED. Used by everything that belongs to one supplier code.</summary>
    public int RequireSupplierCode() =>
        _tenant.SupplierCodeId
        ?? throw new BusinessException(ErrorCodes.SupplierCodeNotSelected,
            "Select a supplier code first.",
            "Pick the supplier code to work in with POST /api/auth/selectsuppliercode.");

    /// <summary>
    /// The company the caller works in, for company-wise data (users, roles, supplier codes).
    /// Super Admin without a picked supplier code gets null, meaning "not limited to one company".
    /// Everyone else always has a company; a user without one is refused.
    /// </summary>
    public async Task<int?> CompanyScopeAsync(CancellationToken ct)
    {
        if (await IsSuperAdminAsync(ct))
        {
            return _tenant.CompanyId;
        }

        return _tenant.CompanyId
               ?? throw new ForbiddenException(ErrorCodes.PermissionDenied,
                   "Your account is not linked to a company.", "Ask the administrator to assign your account to a company.");
    }
}
