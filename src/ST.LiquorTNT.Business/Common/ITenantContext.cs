namespace ST.LiquorTNT.Business.Common;

/// <summary>
/// The company / supplier code / excise of the current session, filled by the API layer from the server-side session
/// (the supplier code the user picked after login) and the token. Hierarchy: Company → Excise → Supplier Code.
/// These values are NEVER read from the request body, query string or a client header.
/// </summary>
public interface ITenantContext
{
    /// <summary>Company of the active supplier code; without a supplier code, the user's home company (null for Super Admin).</summary>
    int? CompanyId { get; }

    /// <summary>The supplier code (SUPPLIER_CODE.ID) the session works in; null until the user picks one.</summary>
    int? SupplierCodeId { get; }

    /// <summary>Excise of the active supplier code (RJ, UP ...); null until a supplier code is picked.</summary>
    string? ExciseCode { get; }
}
