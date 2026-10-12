using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct);

    Task<UserResponse> GetByIdAsync(int id, CancellationToken ct);

    Task<PagedResponse<UserResponse>> GetPageAsync(UserListRequest request, CancellationToken ct);

    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct);

    Task<UserResponse> ActivateAsync(int id, CancellationToken ct);

    Task<UserResponse> DeactivateAsync(int id, CancellationToken ct);

    Task<UserResponse> UnlockAsync(int id, CancellationToken ct);

    /// <summary>Admin gives the user a temporary password; the user's security question is removed.</summary>
    Task<UserResponse> ResetPasswordAsync(int id, ResetUserPasswordRequest request, CancellationToken ct);
}
