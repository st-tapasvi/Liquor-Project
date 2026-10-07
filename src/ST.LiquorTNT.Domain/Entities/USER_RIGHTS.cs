namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>USER_RIGHTS</c>: a custom right given straight to one user, on top of their roles.
/// Custom rights only ADD; they never take away a right that a role gives.
/// <see cref="SupplierCodeId"/> null means every supplier code of the user's company.
/// </summary>
public class USER_RIGHTS
{
    private USER_RIGHTS()
    {
    }

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int PageActionId { get; private set; }
    public int? SupplierCodeId { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public static USER_RIGHTS Create(int userId, int pageActionId, int? supplierCodeId, DateTime now, int? createdBy) => new()
    {
        UserId = userId,
        PageActionId = pageActionId,
        SupplierCodeId = supplierCodeId,
        CreatedAt = now,
        CreatedBy = createdBy,
    };
}
