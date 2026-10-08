using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Roles;

namespace ST.LiquorTNT.Business.Roles;

public interface IRoleService
{
    /// <summary>The roles of the caller's company; with <paramref name="supplierCodeId"/>, only those usable in that supplier code.</summary>
    Task<IReadOnlyList<RoleResponse>> GetListAsync(int? supplierCodeId, CancellationToken ct);

    Task<RoleResponse> GetByIdAsync(int id, CancellationToken ct);

    Task<RoleResponse> CreateAsync(SaveRoleRequest request, CancellationToken ct);

    Task<RoleResponse> UpdateAsync(int id, SaveRoleRequest request, CancellationToken ct);

    Task<MessageResponse> DeleteAsync(int id, CancellationToken ct);

    Task<RoleRightsResponse> GetRightsAsync(int id, CancellationToken ct);

    Task<RoleRightsResponse> UpdateRightsAsync(int id, UpdateRoleRightsRequest request, CancellationToken ct);

    /// <summary>Every page with its actions: the empty rights grid.</summary>
    Task<IReadOnlyList<PageResponse>> GetPagesAsync(CancellationToken ct);
}
