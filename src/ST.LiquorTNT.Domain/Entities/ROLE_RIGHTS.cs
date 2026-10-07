namespace ST.LiquorTNT.Domain.Entities;

/// <summary>Entity for <c>ROLE_RIGHTS</c>: one row = this role has this page action (e.g. Agent Manager → user.add).</summary>
public class ROLE_RIGHTS
{
    private ROLE_RIGHTS()
    {
    }

    public int Id { get; private set; }
    public int RoleId { get; private set; }
    public int PageActionId { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public static ROLE_RIGHTS Create(int roleId, int pageActionId, DateTime now, int? createdBy) => new()
    {
        RoleId = roleId,
        PageActionId = pageActionId,
        CreatedAt = now,
        CreatedBy = createdBy,
    };
}
