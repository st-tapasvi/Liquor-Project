using System.Globalization;
using ST.LiquorTNT.Business.Common;

namespace ST.LiquorTNT.Api.Security;

/// <summary>
/// Company / supplier code / excise of the current call, never from the request, so a caller cannot ask for another
/// company's data. The supplier code comes from the server-side session (<see cref="SessionScope"/>, filled by
/// SessionValidationMiddleware); without a picked supplier code, the company is the user's home company from the token.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private readonly HttpContext? _http;

    public TenantContext(IHttpContextAccessor accessor) => _http = accessor.HttpContext;

    public int? CompanyId => Scope?.CompanyId ?? HomeCompanyId;

    public int? SupplierCodeId => Scope?.SupplierCodeId;

    public string? ExciseCode => Scope?.ExciseCode;

    private SessionScope? Scope =>
        _http is not null && _http.Items.TryGetValue(SessionScope.ItemKey, out var value) ? value as SessionScope : null;

    private int? HomeCompanyId =>
        int.TryParse(_http?.User.FindFirst("company_id")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
}
