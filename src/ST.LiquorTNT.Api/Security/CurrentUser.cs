using System.Globalization;
using System.Security.Claims;
using ST.LiquorTNT.Business.Common;

namespace ST.LiquorTNT.Api.Security;

public sealed class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUser(IHttpContextAccessor accessor) => _principal = accessor.HttpContext?.User;

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated ?? false;

    public int? UserId =>
        int.TryParse(_principal?.FindFirst("sub")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public string? UserName => _principal?.FindFirst("unique_name")?.Value;

    public IReadOnlyCollection<string> Rights =>
        _principal?.FindAll("perm").Select(c => c.Value).ToArray() ?? Array.Empty<string>();
}
