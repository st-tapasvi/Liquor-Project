using ST.LiquorTNT.Contracts.SupplierCodes;

namespace ST.LiquorTNT.Contracts.Access;

/// <summary>Body of <c>POST /api/auth/selectsuppliercode</c>: only the id. The server checks the user really holds it.</summary>
public sealed class SelectSupplierCodeRequest
{
    public int SupplierCodeId { get; set; }
}

/// <summary>
/// What the caller may do right now, for the menu and the buttons (<c>GET /api/auth/mypermissions</c>).
/// The server checks every call on its own; this list only helps the screen hide what would be refused.
/// </summary>
public sealed class MyPermissionsResponse
{
    /// <summary>Super Admin (Sundaram Tech) may do everything; <see cref="Permissions"/> is then the full list.</summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>The supplier code this session works in; null until one is picked.</summary>
    public SupplierCodeResponse? ActiveSupplierCode { get; set; }

    /// <summary>Permission keys such as "user.add".</summary>
    public IReadOnlyCollection<string> Permissions { get; set; } = Array.Empty<string>();
}
