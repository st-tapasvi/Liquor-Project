using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.RoleGroups;

namespace ST.LiquorTNT.Business.RoleGroups;

public interface IRoleGroupService
{
    /// <summary>The role groups of the caller's company.</summary>
    Task<IReadOnlyList<RoleGroupResponse>> GetListAsync(CancellationToken ct);

    Task<RoleGroupResponse> GetByIdAsync(int id, CancellationToken ct);

    Task<RoleGroupResponse> CreateAsync(SaveRoleGroupRequest request, CancellationToken ct);

    Task<RoleGroupResponse> UpdateAsync(int id, SaveRoleGroupRequest request, CancellationToken ct);

    Task<MessageResponse> DeleteAsync(int id, CancellationToken ct);

    /// <summary>Sets the FULL list of roles of the group; applies to every user of the group on their next call.</summary>
    Task<RoleGroupResponse> UpdateRolesAsync(int id, UpdateRoleGroupRolesRequest request, CancellationToken ct);
}
