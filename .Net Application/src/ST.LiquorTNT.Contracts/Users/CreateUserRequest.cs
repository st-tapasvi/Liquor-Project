namespace ST.LiquorTNT.Contracts.Users;

public sealed class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? CompanyId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmployeeCode { get; set; }

    /// <summary>True (default) when the administrator sets a temporary password the user must replace at first login.</summary>
    public bool ForcePasswordChange { get; set; } = true;
}
