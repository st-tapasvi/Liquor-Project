using System.Linq.Expressions;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Business.Users;

/// <summary>
/// The one definition of USERS → <see cref="UserResponse"/>. As an expression it runs inside the
/// database query (list endpoint); compiled, it maps a loaded entity. Never exposes the hash.
/// </summary>
public static class UserProjections
{
    public static readonly Expression<Func<USERS, UserResponse>> ToResponse = u => new UserResponse
    {
        Id = u.Id,
        UserName = u.UserName,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        EmployeeCode = u.EmployeeCode,
        RoleId = u.RoleId,
        CompanyId = u.CompanyId,
        IsActive = u.IsActive,
        IsBlocked = u.IsBlocked,
        FailedLoginAttempts = u.FailedLoginAttempts,
        LockedUntil = u.LockedUntil,
        ForcePasswordChange = u.ForcePasswordChange,
        PasswordExpiresAt = u.PasswordExpiresAt,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt,
    };

    private static readonly Func<USERS, UserResponse> Compiled = ToResponse.Compile();

    public static UserResponse Map(USERS user) => Compiled(user);
}
