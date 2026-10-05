using System.Security.Cryptography;

namespace ST.LiquorTNT.Api.Security;

/// <summary>Settings of the browser cookies, bound from <c>Auth:Cookie</c>.</summary>
public sealed class AuthCookieOptions
{
    public const string Section = "Auth:Cookie";

    /// <summary>
    /// "Lax" (default) when the web application is served from the API's own origin (or the Vite dev
    /// proxy); "None" only for a split deployment where the page and the API are on different sites.
    /// </summary>
    public string SameSite { get; set; } = "Lax";
}

/// <summary>
/// The browser's two cookies, both issued by the API at login and removed at logout:
/// <list type="bullet">
/// <item><c>jwt</c> - the access token. HttpOnly, so page script can never read it; the browser attaches
/// it on its own and the JWT handler reads it when there is no Authorization header.</item>
/// <item><c>XSRF-TOKEN</c> - a random value the page MAY read. Axios echoes it in <c>X-XSRF-TOKEN</c> and
/// <see cref="Middleware.CsrfProtectionMiddleware"/> requires the two to match on cookie-authenticated writes.</item>
/// </list>
/// Both carry <c>Path=/</c>, <c>Secure</c> (on https and on localhost, where browsers accept it) and the same
/// Max-Age as the session's hard limit. The desktop application never sees them: it keeps the bearer header.
/// </summary>
public static class AuthCookies
{
    public const string JwtCookieName = "jwt";
    public const string CsrfCookieName = "XSRF-TOKEN";
    public const string CsrfHeaderName = "X-XSRF-TOKEN";

    public static void Issue(HttpContext context, string accessToken, TimeSpan lifetime, AuthCookieOptions options)
    {
        var maxAge = lifetime > TimeSpan.Zero ? lifetime : (TimeSpan?)null;

        context.Response.Cookies.Append(JwtCookieName, accessToken, Build(context, options, httpOnly: true, maxAge));
        context.Response.Cookies.Append(CsrfCookieName, NewCsrfToken(), Build(context, options, httpOnly: false, maxAge));
    }

    public static void Clear(HttpContext context, AuthCookieOptions options)
    {
        // Same attributes as on write, or the browser keeps the original cookie.
        context.Response.Cookies.Delete(JwtCookieName, Build(context, options, httpOnly: true, maxAge: null));
        context.Response.Cookies.Delete(CsrfCookieName, Build(context, options, httpOnly: false, maxAge: null));
    }

    public static string? ReadJwt(HttpRequest request) =>
        request.Cookies.TryGetValue(JwtCookieName, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public static string? ReadCsrfCookie(HttpRequest request) =>
        request.Cookies.TryGetValue(CsrfCookieName, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string NewCsrfToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static CookieOptions Build(HttpContext context, AuthCookieOptions options, bool httpOnly, TimeSpan? maxAge) => new()
    {
        HttpOnly = httpOnly,
        // Browsers refuse a Secure cookie set over plain http - except on localhost. A plain-http LAN
        // install therefore gets a non-Secure cookie rather than no cookie at all.
        Secure = context.Request.IsHttps || context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
        SameSite = options.SameSite.Equals("None", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.None
                 : options.SameSite.Equals("Strict", StringComparison.OrdinalIgnoreCase) ? SameSiteMode.Strict
                 : SameSiteMode.Lax,
        Path = "/",
        MaxAge = maxAge,
        IsEssential = true,
    };
}
