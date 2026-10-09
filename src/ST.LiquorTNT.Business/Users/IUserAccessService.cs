using ST.LiquorTNT.Contracts.Users;

namespace ST.LiquorTNT.Business.Users;

/// <summary>A user's master roles (per supplier code) and custom rights (per supplier code).</summary>
public interface IUserAccessService
{
    Task<UserAccessResponse> GetAsync(int userId, CancellationToken ct);

    Task<UserAccessResponse> UpdateRolesAsync(int userId, UpdateUserRolesRequest request, CancellationToken ct);

    Task<UserAccessResponse> UpdateRoleGroupsAsync(int userId, UpdateUserRoleGroupsRequest request, CancellationToken ct);

    Task<UserAccessResponse> UpdateRightsAsync(int userId, UpdateUserRightsRequest request, CancellationToken ct);
}
