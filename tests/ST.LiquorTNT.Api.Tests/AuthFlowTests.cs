using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Common;
using ST.LiquorTNT.Contracts.Users;
using Xunit;
using static ST.LiquorTNT.Api.Tests.ApiTestHelpers;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>
/// End-to-end over HTTP against the real API and the local dev MySQL: the user module as a tester
/// would exercise it. Creates its own throw-away user (e2e_xxxx) so the admin account is never locked.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuthFlowTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthFlowTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCode(response)).Should().Be("UNAUTHENTICATED");       // the challenge is RFC 7807 too
        response.Headers.Should().ContainKey("X-Correlation-Id");
    }

    [Fact]
    public async Task FullFlow_Login_Lockout_Unlock_Logout_SessionLimit()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();

        // 1. admin logs in
        var admin = await LoginOk(client, AdminUser, AdminPassword);
        var me = await GetWithToken<CurrentUserResponse>(client, "/api/auth/me", admin.AccessToken);
        me.UserName.Should().Be(AdminUser);

        // 2. admin creates a temporary user (not forced to change, so we can log in directly)
        var tempName = NewTempUserName();
        var temp = await CreateTempUserAsync(client, admin.AccessToken, tempName);

        try
        {
            // 3. two wrong passwords -> 401, third -> locked 403, then even the right one is refused
            (await LoginFail(client, tempName, "wrong-1")).Should().Be((HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS"));
            (await LoginFail(client, tempName, "wrong-2")).Should().Be((HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS"));
            (await LoginFail(client, tempName, "wrong-3")).Should().Be((HttpStatusCode.Forbidden, "USER_LOCKED"));
            (await LoginFail(client, tempName, StrongPassword)).Should().Be((HttpStatusCode.Forbidden, "USER_LOCKED"));

            var locked = await GetWithToken<UserResponse>(client, $"/api/users/{temp.Id}", admin.AccessToken);
            locked.LockedUntil.Should().NotBeNull();
            locked.FailedLoginAttempts.Should().Be(3);

            // 4. admin unlocks -> the response shows the cleared state, and login works
            var unlock = await PostJson(client, $"/api/users/{temp.Id}/unlock", null, admin.AccessToken);
            unlock.StatusCode.Should().Be(HttpStatusCode.OK);
            var unlocked = (await unlock.Content.ReadFromJsonAsync<UserResponse>())!;
            unlocked.LockedUntil.Should().BeNull();
            unlocked.FailedLoginAttempts.Should().Be(0);
            var first = await LoginOk(client, tempName, StrongPassword);

            // 5. logout ends the session: the same token is now refused
            var loggedOut = await Logout(client, first.AccessToken);
            loggedOut.StatusCode.Should().Be(HttpStatusCode.OK);
            (await loggedOut.Content.ReadFromJsonAsync<MessageResponse>())!.Message.Should().Be("Logged out.");
            var afterLogout = await client.SendAsync(WithToken(HttpMethod.Get, "/api/auth/me", first.AccessToken));
            afterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await ErrorCode(afterLogout)).Should().Be("SESSION_INVALID");

            // 6. session limit (MAX_ACTIVE_SESSIONS = 2, REJECT): third concurrent login is refused
            var s1 = await LoginOk(client, tempName, StrongPassword);
            var s2 = await LoginOk(client, tempName, StrongPassword);
            (await LoginFail(client, tempName, StrongPassword)).Should().Be((HttpStatusCode.Conflict, "SESSION_LIMIT_REACHED"));

            var sessions = await GetWithToken<List<SessionResponse>>(client, "/api/auth/sessions", s1.AccessToken);
            sessions.Should().HaveCount(2);
            sessions.Should().ContainSingle(s => s.IsCurrent);

            // 7. revoking the other device frees a slot
            var other = sessions.Single(s => !s.IsCurrent);
            (await client.SendAsync(WithToken(HttpMethod.Delete, $"/api/auth/sessions/{other.Id}", s1.AccessToken))).StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.SendAsync(WithToken(HttpMethod.Get, "/api/auth/me", s2.AccessToken))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await LoginOk(client, tempName, StrongPassword);

            // 8. deactivating the user ends every session and blocks login
            var deactivate = await PostJson(client, $"/api/users/{temp.Id}/deactivate", null, admin.AccessToken);
            deactivate.StatusCode.Should().Be(HttpStatusCode.OK);
            (await deactivate.Content.ReadFromJsonAsync<UserResponse>())!.IsActive.Should().BeFalse();
            (await client.SendAsync(WithToken(HttpMethod.Get, "/api/auth/me", s1.AccessToken))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await LoginFail(client, tempName, StrongPassword)).Should().Be((HttpStatusCode.Forbidden, "USER_INACTIVE"));
        }
        finally
        {
            await Logout(client, admin.AccessToken);
            await DeleteUserAsync(_factory, tempName);      // never leave an e2e user (with a known password) behind
        }
    }

    [Fact]
    public async Task ForcedPasswordChange_BlocksLoginUntilChanged()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var admin = await LoginOk(client, AdminUser, AdminPassword);
        var tempName = NewTempUserName();
        var temp = await CreateTempUserAsync(client, admin.AccessToken, tempName, forcePasswordChange: true);

        try
        {
            (await LoginFail(client, tempName, StrongPassword)).Should().Be((HttpStatusCode.Forbidden, "PASSWORD_CHANGE_REQUIRED"));

            // same password again is refused by the policy's history rule
            var reuse = await client.PostAsJsonAsync("/api/auth/changepassword",
                new ChangePasswordRequest { UserName = tempName, CurrentPassword = StrongPassword, NewPassword = StrongPassword });
            reuse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var changed = await client.PostAsJsonAsync("/api/auth/changepassword",
                new ChangePasswordRequest { UserName = tempName, CurrentPassword = StrongPassword, NewPassword = "Chang3d!Passw0rd#2" });
            changed.StatusCode.Should().Be(HttpStatusCode.OK, await changed.Content.ReadAsStringAsync());

            var login = await LoginOk(client, tempName, "Chang3d!Passw0rd#2");
            login.User.ForcePasswordChange.Should().BeFalse();
            await Logout(client, login.AccessToken);
        }
        finally
        {
            await Logout(client, admin.AccessToken);
            await DeleteUserAsync(_factory, tempName);
        }
    }

    [Fact]
    public async Task ErrorResponses_KeepCorrelationHeader_AndCarryErrorCode()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var admin = await LoginOk(client, AdminUser, AdminPassword);

        try
        {
            // 404 from the middleware keeps the correlation id that was set before the controller ran
            var notFound = await client.SendAsync(WithToken(HttpMethod.Get, "/api/users/999999999", admin.AccessToken));
            notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);
            notFound.Headers.Should().ContainKey("X-Correlation-Id");
            (await ErrorCode(notFound)).Should().Be("NOT_FOUND");

            // a body that cannot be bound is still RFC 7807 with our errorCode, not MVC's default shape
            var malformed = WithToken(HttpMethod.Post, "/api/users", admin.AccessToken);
            malformed.Content = new StringContent("{\"roleId\":\"abc\"}", System.Text.Encoding.UTF8, "application/json");
            var bad = await client.SendAsync(malformed);
            bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ErrorCode(bad)).Should().Be("VALIDATION_FAILED");
            bad.Headers.Should().ContainKey("X-Correlation-Id");

            // an administrator cannot deactivate their own account
            var self = await PostJson(client, $"/api/users/{admin.User.UserId}/deactivate", null, admin.AccessToken);
            self.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ErrorCode(self)).Should().Be("CANNOT_DEACTIVATE_SELF");
        }
        finally
        {
            await Logout(client, admin.AccessToken);
        }
    }

    [Fact]
    public async Task CreateUser_WeakPassword_400_OnPasswordField()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var admin = await LoginOk(client, AdminUser, AdminPassword);

        try
        {
            var response = await PostJson(client, "/api/users", new CreateUserRequest
            {
                UserName = NewTempUserName(), Password = "short", Roles = new() { new UserRoleAssignment { RoleId = SuperAdminRoleId } },
            }, admin.AccessToken);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var body = await Problem(response);
            body.RootElement.GetProperty("errorCode").GetString().Should().Be("VALIDATION_FAILED");
            body.RootElement.GetProperty("errors").TryGetProperty("password", out var messages).Should().BeTrue();
            messages.GetArrayLength().Should().BeGreaterThan(0);
        }
        finally
        {
            await Logout(client, admin.AccessToken);
        }
    }
}
