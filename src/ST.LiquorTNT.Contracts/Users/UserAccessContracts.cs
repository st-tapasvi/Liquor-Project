namespace ST.LiquorTNT.Contracts.Users;

/// <summary>
/// A role given to a user. The supplier code comes with the role ("Operator RJ CL 772"); a company-level role
/// (Plant Admin, Agent Manager) covers every supplier code of the company.
/// </summary>
public sealed class UserRoleAssignment
{
    public int RoleId { get; set; }
}

/// <summary>A custom right given to a user for one supplier code, or for every supplier code when <see cref="SupplierCodeId"/> is null.</summary>
public sealed class UserRightAssignment
{
    public int PageActionId { get; set; }
    public int? SupplierCodeId { get; set; }
}

/// <summary>
/// Everything a user holds: roles given directly, role groups (with the roles inside each), and custom rights.
/// The user's rights come from the direct roles AND the roles of the groups.
/// </summary>
public sealed class UserAccessResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    /// <summary>Roles given to the user directly (not the ones that come through a group).</summary>
    public IReadOnlyCollection<UserRoleResponse> Roles { get; set; } = Array.Empty<UserRoleResponse>();

    /// <summary>Role groups given to the user, each with its roles.</summary>
    public IReadOnlyCollection<UserRoleGroupResponse> RoleGroups { get; set; } = Array.Empty<UserRoleGroupResponse>();
    public IReadOnlyCollection<UserRightResponse> Rights { get; set; } = Array.Empty<UserRightResponse>();
}

/// <summary>A role group the user holds.</summary>
public sealed class UserRoleGroupResponse
{
    public int RoleGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public IReadOnlyCollection<UserRoleResponse> Roles { get; set; } = Array.Empty<UserRoleResponse>();
}

public sealed class UserRoleResponse
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    /// <summary>"Operator RJ CL 772", or just the name for a company-level role.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The role's supplier code; null for a company-level role.</summary>
    public int? SupplierCodeId { get; set; }

    /// <summary>"RJ CL 550", or "All supplier codes" when the role covers the whole company.</summary>
    public string SupplierCodeName { get; set; } = string.Empty;
}

public sealed class UserRightResponse
{
    public int PageActionId { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public int? SupplierCodeId { get; set; }
    public string SupplierCodeName { get; set; } = string.Empty;
}

/// <summary>
/// The FULL list of the user's direct roles. Anything not in the list is taken away. May be empty when the user holds
/// a role group (a user needs at least one role or one group).
/// </summary>
public sealed class UpdateUserRolesRequest
{
    public List<UserRoleAssignment> Roles { get; set; } = new();
}

/// <summary>
/// The FULL list of the user's role groups. Anything not in the list is taken away. May be empty when the user has
/// direct roles (a user needs at least one role or one group).
/// </summary>
public sealed class UpdateUserRoleGroupsRequest
{
    public List<int> RoleGroupIds { get; set; } = new();
}

/// <summary>The FULL list of the user's custom rights. Anything not in the list is taken away.</summary>
public sealed class UpdateUserRightsRequest
{
    public List<UserRightAssignment> Rights { get; set; } = new();
}
