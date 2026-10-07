namespace ST.LiquorTNT.Contracts.Users;

/// <summary>Profile fields only. Roles and rights change through PUT /api/users/{id}/roles and /rights.</summary>
public sealed class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmployeeCode { get; set; }
}
