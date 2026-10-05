using ST.LiquorTNT.Api.Middleware;
using ST.LiquorTNT.Business.Common;

namespace ST.LiquorTNT.Api.Security;

/// <summary>Reads request facts from the HttpContext for auditing and session handling.</summary>
public sealed class RequestContext : IRequestContext
{
    private const string BearerPrefix = "Bearer ";

    private readonly HttpContext? _http;

    public RequestContext(IHttpContextAccessor accessor) => _http = accessor.HttpContext;

    public string? IpAddress => _http?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => Truncate(_http?.Request.Headers.UserAgent.ToString(), 255);

    public string? CorrelationId =>
        _http is not null && _http.Items.TryGetValue(CorrelationMiddleware.ItemKey, out var value)
            ? value as string
            : null;

    public string? AccessToken
    {
        get
        {
            if (_http is null)
            {
                return null;
            }

            var header = _http.Request.Headers.Authorization.ToString();
            if (header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = header[BearerPrefix.Length..].Trim();
                return token.Length > 0 ? token : null;
            }

            return AuthCookies.ReadJwt(_http.Request);
        }
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? null : value.Length <= max ? value : value[..max];
}
