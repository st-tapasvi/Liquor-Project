namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>COMPANY</c>. The User module only checks that a company exists and is active
/// when it is assigned to a user; the company master is a separate module.
/// </summary>
public class COMPANY
{
    private COMPANY()
    {
    }

    public int Id { get; private set; }
    public string? CompanyName { get; private set; }
    public bool IsActive { get; private set; }
}
