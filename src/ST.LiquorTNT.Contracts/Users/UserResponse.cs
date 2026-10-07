namespace ST.LiquorTNT.Contracts.Users;

public sealed class UserResponse
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmployeeCode { get; set; }
    public int? CompanyId { get; set; }
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    /// <summary>Wrong passwords counted today; 0 after a successful login or an unlock.</summary>
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool ForcePasswordChange { get; set; }
    public DateTime? PasswordExpiresAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}
