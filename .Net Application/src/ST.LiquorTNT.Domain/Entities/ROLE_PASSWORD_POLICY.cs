namespace ST.LiquorTNT.Domain.Entities;

/// <summary>Entity for <c>ROLE_PASSWORD_POLICY</c>: which password policy applies to a role (one per role).</summary>
public class ROLE_PASSWORD_POLICY
{
    private ROLE_PASSWORD_POLICY()
    {
    }

    public int Id { get; private set; }
    public int RoleId { get; private set; }
    public int PasswordPolicyId { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}
