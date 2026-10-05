using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using ST.LiquorTNT.Api.Security;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;

namespace ST.LiquorTNT.Api.Middleware;

/// <summary>
/// Cross-site request forgery guard for the browser, which authenticates with the HttpOnly <c>jwt</c> cookie.
/// SameSite=Lax is the first layer; this is the second, so neither stands alone.
///
/// Applies to POST/PUT/PATCH/DELETE on endpoints that need a session (anonymous endpoints are not
/// authorised by the cookie, so there is nothing to forge) when the request carries the jwt cookie and
/// no Authorization header - that is, when the cookie is what authenticates it. Then:
/// <list type="number">
/// <item>the <c>X-XSRF-TOKEN</c> header must equal the <c>XSRF-TOKEN</c> cookie (double submit: a page on
/// another site cannot read our cookie, so it cannot produce the header);</item>
/// <item>an <c>Origin</c> header, when the browser sends one, must be this host or a configured CORS origin.</item>
/// </list>
/// A request with <c>Authorization: Bearer</c> (desktop application, scripts, tests) is never checked:
/// a browser does not add that header on another site's behalf.
/// </summary>
public sealed class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;

    public CsrfProtectionMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _allowedOrigins = new HashSet<string>(
            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        if (!IsStateChanging(request.Method)
            || !request.Path.StartsWithSegments("/api")
            || request.Headers.ContainsKey("Authorization")
            || AuthCookies.ReadJwt(request) is null
            || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return _next(context);
        }

        var origin = request.Headers.Origin.ToString();
        if (origin.Length > 0 && !IsOwnOrigin(request, origin) && !_allowedOrigins.Contains(origin))
        {
            throw new ForbiddenException(ErrorCodes.CsrfRejected, "The request came from another site.",
                $"Origin '{origin}' may not call {request.Method} {request.Path}.");
        }

        var cookie = AuthCookies.ReadCsrfCookie(request);
        var header = request.Headers[AuthCookies.CsrfHeaderName].ToString();
        if (cookie is null || header.Length == 0 || !FixedTimeEquals(cookie, header))
        {
            throw new ForbiddenException(ErrorCodes.CsrfRejected, "The request is missing a valid CSRF token.",
                $"Cookie-authenticated calls must send the {AuthCookies.CsrfCookieName} cookie's value in the {AuthCookies.CsrfHeaderName} header.");
        }

        return _next(context);
    }

    private static bool IsStateChanging(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    /// <summary>The Origin names this very host (host[:port] compare). Uri.Authority drops a default port exactly like the Host header does.</summary>
    private static bool IsOwnOrigin(HttpRequest request, string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
}
