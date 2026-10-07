using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Roles;

namespace ST.LiquorTNT.Business.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponse>> GetListAsync(CancellationToken ct);

    Task<RoleResponse> GetByIdAsync(int id, CancellationToken ct);

    Task<RoleResponse> CreateAsync(SaveRoleRequest request, CancellationToken ct);

    Task<RoleResponse> UpdateAsync(int id, SaveRoleRequest request, CancellationToken ct);

    Task<MessageResponse> DeleteAsync(int id, CancellationToken ct);

    Task<RoleRightsResponse> GetRightsAsync(int id, CancellationToken ct);

    Task<RoleRightsResponse> UpdateRightsAsync(int id, UpdateRoleRightsRequest request, CancellationToken ct);

    /// <summary>Every page with its actions: the empty rights grid.</summary>
    Task<IReadOnlyList<PageResponse>> GetPagesAsync(CancellationToken ct);
}
