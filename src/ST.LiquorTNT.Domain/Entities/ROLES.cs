namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>ROLES</c>: a "master role" - a named bundle of rights (Operator, Supervisor ...).
/// Roles are company-wise. Three special kinds have no company:
/// <list type="bullet">
/// <item><see cref="IsSystem"/> - Super Admin (Sundaram Tech): every right in every company. Cannot be edited or deleted.</item>
/// <item><see cref="IsTemplate"/> - a default role, copied into each new company, which may then change it freely.</item>
/// </list>
/// <see cref="IsAdminRole"/> marks Plant Admin: its holders are "admin users" that only a holder of
/// <c>user.manageadmin</c> may create, edit or assign.
/// </summary>
public class ROLES
{
    private ROLES()
    {
        RoleName = string.Empty;
    }

    public int Id { get; private set; }
    public int? CompanyId { get; private set; }
    public string RoleName { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsTemplate { get; private set; }
    public bool IsAdminRole { get; private set; }
    public bool IsActive { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>A role that belongs to a company (<paramref name="companyId"/>) or, when null, a new default template.</summary>
    public static ROLES Create(int? companyId, string roleName, string? description, bool isAdminRole, DateTime now, int? createdBy)
    {
        var role = new ROLES
        {
            CompanyId = companyId,
            IsTemplate = companyId is null,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = createdBy,
        };

        role.Update(roleName, description, isAdminRole, now, createdBy);
        return role;
    }

    /// <summary>A company's own copy of a default template; the copy no longer depends on the template.</summary>
    public static ROLES CopyOf(ROLES template, int companyId, DateTime now, int? createdBy)
    {
        if (!template.IsTemplate)
        {
            throw new InvalidOperationException("Only a default template can be copied into a company.");
        }

        return Create(companyId, template.RoleName, template.Description, template.IsAdminRole, now, createdBy);
    }

    public void Update(string roleName, string? description, bool isAdminRole, DateTime now, int? updatedBy)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("The Super Admin role cannot be changed.");
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name is required.", nameof(roleName));
        }

        RoleName = roleName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsAdminRole = isAdminRole;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }
}
