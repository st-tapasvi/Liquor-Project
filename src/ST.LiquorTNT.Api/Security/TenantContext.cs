using System.Globalization;
using System.Security.Claims;
using ST.LiquorTNT.Business.Common;

namespace ST.LiquorTNT.Api.Security;

/// <summary>
/// Company / plant / excise come from the token only, never from the request,
/// so a caller cannot ask for another company's data.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private readonly ClaimsPrincipal? _principal;

    public TenantContext(IHttpContextAccessor accessor) => _principal = accessor.HttpContext?.User;

    public int? CompanyId => ReadInt("company_id");

    public int? PlantId => ReadInt("plant_id");

    public string? ExciseCode => _principal?.FindFirst("excise_code")?.Value;

    private int? ReadInt(string claimType) =>
        int.TryParse(_principal?.FindFirst(claimType)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}
