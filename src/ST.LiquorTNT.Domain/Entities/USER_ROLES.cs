namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>USER_ROLES</c>: a user holds a role, e.g. Ramesh → "Operator RJ CL 550". The supplier code comes from the
/// role (<see cref="ROLES.SupplierCodeId"/>); a company-level role covers every supplier code of the user's company.
/// </summary>
public class USER_ROLES
{
    private USER_ROLES()
    {
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int RoleId { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public static USER_ROLES Create(int userId, int roleId, DateTime now, int? createdBy) => new()
    {
        UserId = userId,
        RoleId = roleId,
        CreatedAt = now,
        CreatedBy = createdBy,
    };
}
