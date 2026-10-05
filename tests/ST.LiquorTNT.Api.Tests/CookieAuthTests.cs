using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ST.LiquorTNT.Contracts.Auth;
using Xunit;
using static ST.LiquorTNT.Api.Tests.ApiTestHelpers;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>
/// The browser path: the JWT travels in the HttpOnly <c>jwt</c> cookie, writes need the double-submit CSRF
/// token, and the bearer path used by the desktop application and the other tests is untouched.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CookieAuthTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public CookieAuthTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>A client that keeps cookies like a browser, on https so Secure cookies are sent back.</summary>
    private HttpClient Browser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });

    private static Task<HttpResponseMessage> BrowserLogin(HttpClient client) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest { UserName = AdminUser, Password = AdminPassword });

    private static string SetCookie(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(name + "=", StringComparison.Ordinal));

    private static string CookieValue(string setCookie) => Regex.Match(setCookie, "^[^=]+=([^;]*)").Groups[1].Value;

    private static HttpRequestMessage Post(string url, string? csrfToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (csrfToken is not null)
        {
            request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        }

        return request;
    }

    [Fact]
    public async Task Login_SetsHttpOnlyJwtCookie_AndReadableCsrfCookie_AndStillReturnsToken()
    {
        await ResetAdminAsync(_factory);
        using var client = Browser();

        var response = await BrowserLogin(client);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        login.AccessToken.Should().NotBeNullOrEmpty("the desktop application still reads the token from the body");

        var jwt = SetCookie(response, "jwt");
        jwt.Should().ContainEquivalentOf("httponly").And.ContainEquivalentOf("secure").And.ContainEquivalentOf("samesite=lax")
           .And.ContainEquivalentOf("path=/").And.ContainEquivalentOf("max-age=");
        CookieValue(jwt).Should().Be(login.AccessToken, "the cookie carries the very same JWT");

        var csrf = SetCookie(response, "XSRF-TOKEN");
        csrf.Should().NotContainEquivalentOf("httponly", "the page must be able to read it").And.ContainEquivalentOf("secure");
        CookieValue(csrf).Should().NotBeNullOrEmpty().And.NotBe(login.AccessToken);

        // Tab 2 of the same browser: the cookie alone authenticates GET /me, no header involved.
        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK, await me.Content.ReadAsStringAsync());
        (await me.Content.ReadFromJsonAsync<CurrentUserResponse>())!.UserName.Should().Be(AdminUser);

        await client.SendAsync(Post("/api/auth/logout", CookieValue(csrf)));
    }

    [Fact]
    public async Task CookieWrite_NeedsMatchingCsrfHeader_ThenLogoutClearsCookiesAndEndsTheSession()
    {
        await ResetAdminAsync(_factory);
        using var client = Browser();
        var login = await BrowserLogin(client);
        var csrfToken = CookieValue(SetCookie(login, "XSRF-TOKEN"));

        // No header: refused, and the session is untouched.
        var noHeader = await client.SendAsync(Post("/api/auth/logout", csrfToken: null));
        noHeader.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(noHeader)).Should().Be("CSRF_REJECTED");

        // Wrong header (an attacker guessing): refused.
        var wrong = await client.SendAsync(Post("/api/auth/logout", "not-the-cookie-value"));
        wrong.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK, "only the writes were refused");

        // Matching header: the session ends and both cookies are expired on the browser.
        var logout = await client.SendAsync(Post("/api/auth/logout", csrfToken));
        logout.StatusCode.Should().Be(HttpStatusCode.OK, await logout.Content.ReadAsStringAsync());
        SetCookie(logout, "jwt").Should().ContainEquivalentOf("expires=");
        SetCookie(logout, "XSRF-TOKEN").Should().ContainEquivalentOf("expires=");

        // Tab 2 after the logout in tab 1: the cookie is gone (or revoked) -> login page.
        var after = await client.GetAsync("/api/auth/me");
        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokedSession_CookieStillInBrowser_401SessionInvalid()
    {
        await ResetAdminAsync(_factory);
        using var tab = Browser();
        var login = await BrowserLogin(tab);
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        // Another client (the desktop, say) ends that same session with the bearer token.
        using var desktop = _factory.CreateClient();
        (await Logout(desktop, token)).StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await tab.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCode(me)).Should().Be("SESSION_INVALID");
    }

    [Fact]
    public async Task BearerHeader_WinsOverCookie_AndIsNeverCsrfChecked()
    {
        await ResetAdminAsync(_factory);
        using var client = Browser();
        using var desktop = _factory.CreateClient();                        // no cookie jar, like the EXE
        var cookieLogin = await BrowserLogin(client);                       // session A in the cookie jar
        var csrfToken = CookieValue(SetCookie(cookieLogin, "XSRF-TOKEN"));
        var bearer = await LoginOk(desktop, AdminUser, AdminPassword);      // session B, token in the body

        try
        {
            // Sent from the browser client, so the request carries cookie A AND header B, no CSRF header:
            // allowed (bearer requests are never CSRF-checked) and it ends B - the header wins over the cookie.
            var logout = await Logout(client, bearer.AccessToken);
            logout.StatusCode.Should().Be(HttpStatusCode.OK, await logout.Content.ReadAsStringAsync());

            (await client.SendAsync(WithToken(HttpMethod.Get, "/api/auth/me", bearer.AccessToken))).StatusCode
                .Should().Be(HttpStatusCode.Unauthorized, "B is over");
            (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK, "A, in the cookie, is still alive");
        }
        finally
        {
            await client.SendAsync(Post("/api/auth/logout", csrfToken));
        }
    }

    [Fact]
    public async Task ForeignOrigin_CookieWrite_403_EvenWithCsrfHeader()
    {
        await ResetAdminAsync(_factory);
        using var client = Browser();
        var login = await BrowserLogin(client);
        var csrfToken = CookieValue(SetCookie(login, "XSRF-TOKEN"));

        try
        {
            var request = Post("/api/auth/logout", csrfToken);
            request.Headers.Add("Origin", "https://evil.example");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ErrorCode(response)).Should().Be("CSRF_REJECTED");
        }
        finally
        {
            await client.SendAsync(Post("/api/auth/logout", csrfToken));
        }
    }

    [Fact]
    public async Task AnonymousWrite_WithStaleCookie_NotCsrfChecked()
    {
        await ResetAdminAsync(_factory);
        using var client = Browser();
        var first = await BrowserLogin(client);
        var csrfToken = CookieValue(SetCookie(first, "XSRF-TOKEN"));

        try
        {
            // A second login while the jwt cookie is present and no X-XSRF-TOKEN is sent: login is
            // anonymous (password-verified), so it must go through and simply re-issue the cookies.
            var second = await BrowserLogin(client);
            second.StatusCode.Should().Be(HttpStatusCode.OK, await second.Content.ReadAsStringAsync());
            csrfToken = CookieValue(SetCookie(second, "XSRF-TOKEN"));
        }
        finally
        {
            await client.SendAsync(Post("/api/auth/logout", csrfToken));
            await ResetAdminAsync(_factory);                                // the first session is still open
        }
    }
}
