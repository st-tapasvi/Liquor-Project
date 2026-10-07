namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>USER_ROLES</c>: a user holds a role for a supplier code, e.g. Ramesh → Operator → RJ CL 550.
/// <see cref="SupplierCodeId"/> null means every supplier code of the user's company (or, for Super Admin, everything).
/// </summary>
public class USER_ROLES
{
    private USER_ROLES()
    {
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int RoleId { get; private set; }
    public int? SupplierCodeId { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public static USER_ROLES Create(int userId, int roleId, int? supplierCodeId, DateTime now, int? createdBy) => new()
    {
        UserId = userId,
        RoleId = roleId,
        SupplierCodeId = supplierCodeId,
        CreatedAt = now,
        CreatedBy = createdBy,
    };
}
