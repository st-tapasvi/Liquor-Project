namespace ST.LiquorTNT.Contracts.RoleGroups;

/// <summary>A role group: a named bundle of master roles of one company, given to users as one piece.</summary>
public sealed class RoleGroupResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    /// <summary>True when the group holds an admin role (Plant Admin): only a holder of user.manageadmin may change or give it.</summary>
    public bool HasAdminRole { get; set; }

    /// <summary>How many users hold the group (a group in use cannot be deleted).</summary>
    public int UserCount { get; set; }

    public IReadOnlyCollection<RoleGroupRoleResponse> Roles { get; set; } = Array.Empty<RoleGroupRoleResponse>();
}

/// <summary>One role inside a group.</summary>
public sealed class RoleGroupRoleResponse
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    /// <summary>"Operator RJ CL 772", or just the name for a company-level role.</summary>
    public string DisplayName { get; set; } = string.Empty;
    public int? SupplierCodeId { get; set; }
    public string? SupplierCodeName { get; set; }
    public bool IsAdminRole { get; set; }
}

/// <summary>Body for creating and for renaming a group (its roles are set with <see cref="UpdateRoleGroupRolesRequest"/>).</summary>
public sealed class SaveRoleGroupRequest
{
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>The FULL list of roles of the group. Anything not in it is taken out — for every user of the group.</summary>
public sealed class UpdateRoleGroupRolesRequest
{
    public List<int> RoleIds { get; set; } = new();
}
