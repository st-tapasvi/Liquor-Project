namespace ST.LiquorTNT.Contracts.Users;

public sealed class UpdateUserRequest
{
    public int RoleId { get; set; }
    public int? CompanyId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmployeeCode { get; set; }
}
