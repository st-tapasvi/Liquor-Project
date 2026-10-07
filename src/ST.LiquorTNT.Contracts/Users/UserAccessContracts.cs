namespace ST.LiquorTNT.Contracts.Users;

/// <summary>A role given to a user for one supplier code, or for every supplier code of the company when <see cref="SupplierCodeId"/> is null.</summary>
public sealed class UserRoleAssignment
{
    public int RoleId { get; set; }
    public int? SupplierCodeId { get; set; }
}

/// <summary>A custom right given to a user for one supplier code, or for every supplier code when <see cref="SupplierCodeId"/> is null.</summary>
public sealed class UserRightAssignment
{
    public int PageActionId { get; set; }
    public int? SupplierCodeId { get; set; }
}

/// <summary>Everything a user holds: roles (per supplier code) and custom rights (per supplier code).</summary>
public sealed class UserAccessResponse
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public IReadOnlyCollection<UserRoleResponse> Roles { get; set; } = Array.Empty<UserRoleResponse>();
    public IReadOnlyCollection<UserRightResponse> Rights { get; set; } = Array.Empty<UserRightResponse>();
}

public sealed class UserRoleResponse
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
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

/// <summary>The FULL list of the user's roles. Anything not in the list is taken away.</summary>
public sealed class UpdateUserRolesRequest
{
    public List<UserRoleAssignment> Roles { get; set; } = new();
}

/// <summary>The FULL list of the user's custom rights. Anything not in the list is taken away.</summary>
public sealed class UpdateUserRightsRequest
{
    public List<UserRightAssignment> Rights { get; set; } = new();
}
