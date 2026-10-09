namespace ST.LiquorTNT.Contracts.Roles;

public sealed class RoleResponse
{
    public int Id { get; set; }

    /// <summary>Null for Super Admin and for the default templates.</summary>
    public int? CompanyId { get; set; }

    /// <summary>The supplier code the role works in. Null = company-level role (every supplier code of the company).</summary>
    public int? SupplierCodeId { get; set; }

    /// <summary>"RJ CL 772", or null for a company-level role.</summary>
    public string? SupplierCodeName { get; set; }

    /// <summary>The name as typed, e.g. "Operator".</summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>What screens show: "Operator RJ CL 772" (role name + supplier code), or just the name for a company-level role.</summary>
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Super Admin: cannot be edited, deleted or assigned by a company.</summary>
    public bool IsSystem { get; set; }

    /// <summary>A default role, copied into new companies / new supplier codes.</summary>
    public bool IsTemplate { get; set; }

    /// <summary>Templates only: copied for every new supplier code (true) or once per company (false).</summary>
    public bool PerSupplierCode { get; set; }

    /// <summary>Plant Admin: its users can only be managed by a holder of user.manageadmin.</summary>
    public bool IsAdminRole { get; set; }
    public bool IsActive { get; set; }

    /// <summary>The password policy users of this role must follow.</summary>
    public int? PasswordPolicyId { get; set; }
}

/// <summary>Body for creating and for editing a role (same fields).</summary>
public sealed class SaveRoleRequest
{
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// The supplier code of the role (one of the caller's company). Null = company-level role. Set on create only:
    /// on edit it must stay the same (create a new role for another supplier code).
    /// </summary>
    public int? SupplierCodeId { get; set; }
    public string? Description { get; set; }

    /// <summary>Admin role (Plant Admin). Always company-level, so not together with a supplier code.</summary>
    public bool IsAdminRole { get; set; }

    /// <summary>Default templates only (Super Admin): copy this template for every new supplier code. Ignored for company roles.</summary>
    public bool PerSupplierCode { get; set; }
    public int PasswordPolicyId { get; set; }
}

/// <summary>A page with all its actions (the rows and columns of the rights grid).</summary>
public sealed class PageResponse
{
    public int PageId { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string? ModuleName { get; set; }

    /// <summary>"WEB" (web application) or "LINE" (line application). Group the rights grid by it.</summary>
    public string ApplicationType { get; set; } = string.Empty;
    public IReadOnlyCollection<PageActionResponse> Actions { get; set; } = Array.Empty<PageActionResponse>();
}

public sealed class PageActionResponse
{
    public int PageActionId { get; set; }
    public string ActionKey { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;

    /// <summary>ANY, ADMIN (needs user.manageadmin to grant) or SYSTEM (never grantable; Super Admin only).</summary>
    public string GrantScope { get; set; } = string.Empty;

    /// <summary>In a role's or user's rights grid: is this action ticked? Always false in the plain page list.</summary>
    public bool Granted { get; set; }
}

/// <summary>A role's rights as a grid: every page, every action, with <see cref="PageActionResponse.Granted"/> set.</summary>
public sealed class RoleRightsResponse
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    /// <summary>"Operator RJ CL 772" — see <see cref="RoleResponse.DisplayName"/>.</summary>
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyCollection<PageResponse> Pages { get; set; } = Array.Empty<PageResponse>();
}

/// <summary>The FULL list of ticked actions. Anything not in the list is removed from the role.</summary>
public sealed class UpdateRoleRightsRequest
{
    public List<int> PageActionIds { get; set; } = new();

    /// <summary>
    /// "WEB" or "LINE": the list is the full ticked list of that application only, and the other application's rights
    /// stay as they are. Empty: the list is the full ticked list of both applications.
    /// </summary>
    public string? ApplicationType { get; set; }
}
