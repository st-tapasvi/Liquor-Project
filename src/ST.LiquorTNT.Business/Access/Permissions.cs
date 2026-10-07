namespace ST.LiquorTNT.Business.Access;

/// <summary>
/// Every permission key the backend checks, in one place. Each key is a row of PAGE_ACTIONS
/// (seeded by db/mysql/009) and is exactly what the frontend receives from GET /api/auth/mypermissions.
/// Format: "&lt;page&gt;.&lt;action&gt;". Add a key here AND in the seed script together.
/// </summary>
public static class Permissions
{
    public const string UserView        = "user.view";
    public const string UserAdd         = "user.add";
    public const string UserEdit        = "user.edit";
    public const string UserStatus      = "user.status";
    public const string UserUnlock      = "user.unlock";
    public const string UserAccess      = "user.access";
    public const string UserManageAdmin = "user.manageadmin";   // admin users, admin roles, ADMIN-scope rights

    public const string RoleView   = "role.view";
    public const string RoleAdd    = "role.add";
    public const string RoleEdit   = "role.edit";
    public const string RoleDelete = "role.delete";

    public const string SecurityConfigView = "securityconfig.view";
    public const string SecurityConfigEdit = "securityconfig.edit";
    public const string PasswordPolicyView = "passwordpolicy.view";
    public const string PasswordPolicyEdit = "passwordpolicy.edit";

    public const string CompanyView = "company.view";           // SYSTEM: Super Admin only

    public const string SupplierCodeView = "suppliercode.view";   // also opens the excise list
    public const string SupplierCodeAdd  = "suppliercode.add";
    public const string SupplierCodeEdit = "suppliercode.edit";

    public const string LiquorCategoryView = "liquorcategory.view";
    public const string LiquorCategoryAdd  = "liquorcategory.add";
    public const string LiquorCategoryEdit = "liquorcategory.edit";
}
