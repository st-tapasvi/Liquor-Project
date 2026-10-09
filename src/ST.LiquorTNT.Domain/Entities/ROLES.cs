namespace ST.LiquorTNT.Domain.Entities;

/// <summary>
/// Entity for <c>ROLES</c>: a "master role" - a named bundle of rights (Operator, Supervisor ...).
/// <list type="bullet">
/// <item>A role of a supplier code (<see cref="SupplierCodeId"/> set) works only in that supplier code, e.g. "Operator"
/// of RJ CL 772, shown as "Operator RJ CL 772". Each supplier code can so have its own rights.</item>
/// <item>A company-level role (<see cref="SupplierCodeId"/> null, e.g. Plant Admin, Agent Manager) covers every supplier
/// code of the company.</item>
/// <item><see cref="IsSystem"/> - Super Admin (Sundaram Tech): every right in every company. No company. Cannot be edited or deleted.</item>
/// <item><see cref="IsTemplate"/> - a default role (no company). <see cref="PerSupplierCode"/> templates are copied for every
/// new supplier code, the others once per company; the company may then change its copies freely.</item>
/// </list>
/// <see cref="IsAdminRole"/> marks Plant Admin: its holders are "admin users" that only a holder of
/// <c>user.manageadmin</c> may create, edit or assign. Admin roles are always company-level.
/// </summary>
public class ROLES
{
    private ROLES()
    {
        RoleName = string.Empty;
    }

    public int Id { get; private set; }
    public int? CompanyId { get; private set; }
    public int? SupplierCodeId { get; private set; }
    public string RoleName { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsTemplate { get; private set; }
    public bool IsAdminRole { get; private set; }

    /// <summary>Templates only: copied for every supplier code (true) or once per company (false).</summary>
    public bool PerSupplierCode { get; private set; }
    public bool IsActive { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// A role of a company (<paramref name="companyId"/>), for one supplier code or, when <paramref name="supplierCodeId"/> is
    /// null, company-level. Without a company it is a new default template.
    /// </summary>
    public static ROLES Create(int? companyId, string roleName, string? description, bool isAdminRole, DateTime now, int? createdBy,
        int? supplierCodeId = null, bool perSupplierCode = false)
    {
        if (supplierCodeId is not null && companyId is null)
        {
            throw new ArgumentException("A template belongs to no supplier code.", nameof(supplierCodeId));
        }

        if (supplierCodeId is not null && isAdminRole)
        {
            throw new ArgumentException("An admin role covers the whole company, so it has no supplier code.", nameof(supplierCodeId));
        }

        var role = new ROLES
        {
            CompanyId = companyId,
            SupplierCodeId = supplierCodeId,
            IsTemplate = companyId is null,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = createdBy,
        };

        role.Update(roleName, description, isAdminRole, now, createdBy, perSupplierCode);
        return role;
    }

    /// <summary>
    /// A company's own copy of a default template; the copy no longer depends on the template. A per-supplier-code
    /// template needs the supplier code to copy it for; a company-level template must not get one.
    /// </summary>
    public static ROLES CopyOf(ROLES template, int companyId, DateTime now, int? createdBy, int? supplierCodeId = null)
    {
        if (!template.IsTemplate)
        {
            throw new InvalidOperationException("Only a default template can be copied into a company.");
        }

        if (template.PerSupplierCode != supplierCodeId.HasValue)
        {
            throw new ArgumentException(template.PerSupplierCode
                ? "This template is copied for a supplier code; give the supplier code."
                : "This template is company-level; it has no supplier code.", nameof(supplierCodeId));
        }

        return Create(companyId, template.RoleName, template.Description, template.IsAdminRole, now, createdBy, supplierCodeId);
    }

    /// <summary>Name, description and admin flag. The supplier code of a role never changes (its users would move with it).</summary>
    public void Update(string roleName, string? description, bool isAdminRole, DateTime now, int? updatedBy, bool perSupplierCode = false)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("The Admin role cannot be changed.");
        }

        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name is required.", nameof(roleName));
        }

        if (isAdminRole && SupplierCodeId is not null)
        {
            throw new ArgumentException("An admin role covers the whole company, so it cannot belong to a supplier code.", nameof(isAdminRole));
        }

        RoleName = roleName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsAdminRole = isAdminRole;
        PerSupplierCode = IsTemplate && perSupplierCode;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }
}
