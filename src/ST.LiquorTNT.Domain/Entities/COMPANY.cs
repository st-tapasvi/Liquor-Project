namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>COMPANY</c>: the customer (e.g. Globus Spirits Ltd). Read-only here: users and supplier codes check
/// that a company exists, and Super Admin lists companies. Companies are created from the CRM; the company master
/// screen is a separate module.
/// </summary>
public class COMPANY
{
    private COMPANY()
    {
    }

    public int Id { get; private set; }
    public string? CompanyName { get; private set; }
    public string? AliasName { get; private set; }     // short name shown in lists: Globus Spirits
    public string? City { get; private set; }
    public bool IsActive { get; private set; }
}
