namespace ST.LiquorTNT.Domain.Entities;

/// <summary>Entity for <c>ROLE_GROUP_ROLES</c>: one row = this group holds this role (e.g. All Operators → Operator RJ CL 772).</summary>
public class ROLE_GROUP_ROLES
{
    private ROLE_GROUP_ROLES()
    {
    }

    public int Id { get; private set; }
    public int RoleGroupId { get; private set; }
    public int RoleId { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public static ROLE_GROUP_ROLES Create(int roleGroupId, int roleId, DateTime now, int? createdBy) => new()
    {
        RoleGroupId = roleGroupId,
        RoleId = roleId,
        CreatedAt = now,
        CreatedBy = createdBy,
    };
}
