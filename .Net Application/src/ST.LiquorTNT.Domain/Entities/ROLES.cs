namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>ROLES</c>. The User module only needs to know a role exists and is active
/// (for the role → password-policy link). Role rights are a separate module.
/// </summary>
public class ROLES
{
    private ROLES()
    {
        RoleName = string.Empty;
    }

    public int Id { get; private set; }
    public string RoleName { get; private set; }
    public bool IsActive { get; private set; }
}
