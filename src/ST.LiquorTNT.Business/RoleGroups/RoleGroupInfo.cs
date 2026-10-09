namespace ST.LiquorTNT.Business.RoleGroups;

/// <summary>A role group as the "who may give what" rules need it: its company, state, roles and whether it holds an admin role.</summary>
public sealed record RoleGroupInfo(int Id, int CompanyId, string GroupName, bool IsActive, IReadOnlyList<int> RoleIds, bool HasAdminRole);
