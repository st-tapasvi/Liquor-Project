using ST.LiquorTNT.Contracts.Roles;
using ST.LiquorTNT.Contracts.SupplierCodes;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Roles;

/// <summary>
/// Data access for roles, their rights and their password policy. Declared here because the Roles feature
/// owns it; implemented in ST.LiquorTNT.Infrastructure.
/// </summary>
public interface IRoleRepository
{
    Task<ROLES?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Roles of one company, projected for the list screen (company-level roles first, then by supplier code and name).
    /// <paramref name="companyId"/> null = the roles that belong to no company: Super Admin and the default templates
    /// (only Super Admin ever sees these). <paramref name="supplierCodeId"/> set = only the roles usable in that supplier
    /// code: its own roles plus the company-level ones.
    /// </summary>
    Task<IReadOnlyList<RoleResponse>> GetListAsync(int? companyId, int? supplierCodeId, CancellationToken ct);

    /// <summary>One role projected like the list (with supplier code name, display name and password policy).</summary>
    Task<RoleResponse?> GetResponseAsync(int roleId, CancellationToken ct);

    /// <summary>
    /// Same name already used for the same supplier code of the same company (or among the company-level roles,
    /// or among the templates)? Case-insensitive. "Operator" may exist once per supplier code.
    /// </summary>
    Task<bool> NameExistsAsync(int? companyId, int? supplierCodeId, string roleName, int? excludeRoleId, CancellationToken ct);

    /// <summary>True when the company already has a company-level role (so its company-level templates were copied before).</summary>
    Task<bool> CompanyHasCompanyRolesAsync(int companyId, CancellationToken ct);

    /// <summary>True when the supplier code already has roles of its own (so its templates were copied before).</summary>
    Task<bool> SupplierCodeHasRolesAsync(int supplierCodeId, CancellationToken ct);

    /// <summary>An ACTIVE supplier code (for checking a new role's supplier code and naming the role), or null.</summary>
    Task<SupplierCodeResponse?> GetSupplierCodeAsync(int supplierCodeId, CancellationToken ct);

    Task<IReadOnlyList<ROLES>> GetTemplatesAsync(CancellationToken ct);

    /// <summary>True when at least one user holds the role (it cannot be deleted then).</summary>
    Task<bool> IsAssignedAsync(int roleId, CancellationToken ct);

    /// <summary>The PAGE_ACTIONS ids the role has.</summary>
    Task<IReadOnlyList<int>> GetRightIdsAsync(int roleId, CancellationToken ct);

    /// <summary>Makes the role's rights exactly <paramref name="pageActionIds"/>: missing rows are added, extra rows removed.</summary>
    Task ReplaceRightsAsync(int roleId, IReadOnlyCollection<int> pageActionIds, DateTime now, int? changedBy, CancellationToken ct);

    /// <summary>Every active page with its active actions, in menu order (the rights grid).</summary>
    Task<IReadOnlyList<PageResponse>> GetPagesAsync(CancellationToken ct);

    /// <summary>The page actions with these ids (unknown ids are simply missing from the result).</summary>
    Task<IReadOnlyList<PAGE_ACTIONS>> GetPageActionsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    Task<ROLE_PASSWORD_POLICY?> GetPolicyLinkAsync(int roleId, CancellationToken ct);

    Task<bool> PasswordPolicyExistsAsync(int passwordPolicyId, CancellationToken ct);

    Task AddAsync(ROLES role, CancellationToken ct);

    Task AddPolicyLinkAsync(ROLE_PASSWORD_POLICY link, CancellationToken ct);

    /// <summary>Deletes the role together with its rights and its password-policy link.</summary>
    Task RemoveAsync(ROLES role, CancellationToken ct);

    /// <summary>Saves; a duplicate role name (unique key) becomes 409 ROLE_NAME_TAKEN.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
