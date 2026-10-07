namespace ST.LiquorTNT.Api.Security;

/// <summary>
/// The supplier code this request works in, read from the server-side session by
/// <see cref="Middleware.SessionValidationMiddleware"/> and handed to <see cref="TenantContext"/> through
/// <c>HttpContext.Items</c>. It never comes from the request itself.
/// </summary>
public sealed record SessionScope(int SupplierCodeId, int CompanyId, string ExciseCode)
{
    public const string ItemKey = "SessionScope";
}
