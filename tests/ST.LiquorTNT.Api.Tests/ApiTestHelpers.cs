using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.LiquorTNT.Business.Auth;
using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.Users;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>Shared plumbing for the end-to-end tests: talk to the API the way a client would.</summary>
internal static class ApiTestHelpers
{
    public const string AdminUser = "admin";
    public const string AdminPassword = "Admin@123";
    public const string StrongPassword = "E2e!Str0ngPass#1";     // satisfies the HARD policy
    public const int SuperAdminRoleId = 1;                       // seeded by db/mysql/009

    /// <summary>Earlier runs may have left admin sessions open (limit is 2) or a lock; start clean.</summary>
    public static async Task ResetAdminAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.Now;

        var admin = await db.USERS.SingleAsync(u => u.UserName == AdminUser);
        admin.Unlock(now, null);

        foreach (var session in await db.USER_SESSION.Where(s => s.UserId == admin.Id && s.Status == USER_SESSION.StatusActive).ToListAsync())
        {
            session.Revoke(now);
        }

        // Without a security question every admin call would be held on the first-login question screen.
        if (!await db.USER_SECURITY_QUESTION.AnyAsync(q => q.UserId == admin.Id && q.IsActive))
        {
            var questionId = await db.SECURITY_QUESTION.Where(q => q.Status).Select(q => q.Id).FirstAsync();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            db.USER_SECURITY_QUESTION.Add(USER_SECURITY_QUESTION.Create(admin.Id, questionId, hasher.Hash(SecurityAnswers.Normalize(E2eAnswer)), now));
        }

        await db.SaveChangesAsync();
    }

    /// <summary>The security answer the helpers give when a login lands on the first-login question screen.</summary>
    public const string E2eAnswer = "e2e answer";

    public static string NewTempUserName() => "e2e_" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>Removes a throw-away user and its dependants (sessions, history, questions, reset requests cascade).</summary>
    public static async Task DeleteUserAsync(WebApplicationFactory<Program> factory, string userName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.USERS.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user is null)
        {
            return;
        }

        db.USERS.Remove(user);
        await db.SaveChangesAsync();
    }

    public static async Task<UserResponse> CreateTempUserAsync(HttpClient client, string adminToken, string userName, bool forcePasswordChange = false)
    {
        var response = await PostJson(client, "/api/users", new CreateUserRequest
        {
            UserName = userName, Password = StrongPassword, FullName = "E2E User", ForcePasswordChange = forcePasswordChange,
            Roles = new() { new UserRoleAssignment { RoleId = SuperAdminRoleId } },     // throw-away users do not need a company
        }, adminToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    public static async Task<LoginResponse> LoginOk(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { UserName = userName, Password = password });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;

        // First login of a fresh user: set the security question, as the screen would, so the session can go on.
        if (login.User.SecurityQuestionRequired)
        {
            var questions = await GetWithToken<List<SecurityQuestionResponse>>(client, "/api/securityquestions", token: null);
            var set = await PutJson(client, "/api/securityquestions/mine", new SetSecurityQuestionRequest
            {
                QuestionId = questions[0].Id, Answer = E2eAnswer, CurrentPassword = password,
            }, login.AccessToken);
            set.StatusCode.Should().Be(HttpStatusCode.OK, await set.Content.ReadAsStringAsync());
        }

        return login;
    }

    public static async Task<(HttpStatusCode Status, string Code)> LoginFail(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { UserName = userName, Password = password });
        return (response.StatusCode, await ErrorCode(response));
    }

    public static async Task<string> ErrorCode(HttpResponseMessage response)
    {
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("errorCode").GetString()!;
    }

    /// <summary>The "message" of a MessageResponse body (logout, verify, reset …).</summary>
    public static async Task<string> MessageOf(HttpResponseMessage response)
    {
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("message").GetString()!;
    }

    public static async Task<JsonDocument> Problem(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

    public static HttpRequestMessage WithToken(HttpMethod method, string url, string? token)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    public static async Task<T> GetWithToken<T>(HttpClient client, string url, string? token)
    {
        var response = await client.SendAsync(WithToken(HttpMethod.Get, url, token));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static Task<HttpResponseMessage> PostJson(HttpClient client, string url, object? body, string? token) =>
        SendJson(client, HttpMethod.Post, url, body, token);

    public static Task<HttpResponseMessage> PutJson(HttpClient client, string url, object? body, string? token) =>
        SendJson(client, HttpMethod.Put, url, body, token);

    private static Task<HttpResponseMessage> SendJson(HttpClient client, HttpMethod method, string url, object? body, string? token)
    {
        var request = WithToken(method, url, token);
        request.Content = JsonContent.Create(body ?? new { });
        return client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> Logout(HttpClient client, string token) => PostJson(client, "/api/auth/logout", null, token);
}
