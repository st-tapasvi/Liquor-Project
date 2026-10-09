namespace ST.LiquorTNT.Contracts.Users;

public sealed class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Only Super Admin sends this (when creating the first user of a company). For everyone else the new user
    /// joins the caller's own company and this value is ignored.
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>Roles given directly. The supplier code comes with each role ("Operator RJ CL 772"); company-level roles cover all.</summary>
    public List<UserRoleAssignment> Roles { get; set; } = new();

    /// <summary>Role groups given to the user. At least one role or one group is needed.</summary>
    public List<int> RoleGroupIds { get; set; } = new();

    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmployeeCode { get; set; }

    /// <summary>True (default) when the administrator sets a temporary password the user must replace at first login.</summary>
    public bool ForcePasswordChange { get; set; } = true;
}
