namespace ST.LiquorTNT.Business.Roles;

/// <summary>How a role is named on screen: "Operator RJ CL 772" for a role of a supplier code, "Plant Admin" for a company-level one.</summary>
public static class RoleNames
{
    public static string Display(string roleName, string? supplierCodeName) =>
        string.IsNullOrEmpty(supplierCodeName) ? roleName : $"{roleName} {supplierCodeName}";
}
