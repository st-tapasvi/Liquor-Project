namespace ST.LiquorTNT.Domain.Entities;

/// <summary>Entity for <c>USER_ROLE_GROUPS</c>: a user holds a role group, and so every role inside it.</summary>
public class USER_ROLE_GROUPS
{
    private USER_ROLE_GROUPS()
    {
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int RoleGroupId { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public static USER_ROLE_GROUPS Create(int userId, int roleGroupId, DateTime now, int? createdBy) => new()
    {
        UserId = userId,
        RoleGroupId = roleGroupId,
        CreatedAt = now,
        CreatedBy = createdBy,
    };
}
