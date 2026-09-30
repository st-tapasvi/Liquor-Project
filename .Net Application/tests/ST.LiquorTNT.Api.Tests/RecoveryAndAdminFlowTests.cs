using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Contracts.PasswordPolicies;
using ST.LiquorTNT.Contracts.SecurityConfig;
using Xunit;
using static ST.LiquorTNT.Api.Tests.ApiTestHelpers;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>Forgot-password recovery and the admin configuration screens, end to end.</summary>
[Collection(ApiCollection.Name)]
public sealed class RecoveryAndAdminFlowTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public RecoveryAndAdminFlowTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ForgotPassword_SetQuestion_Start_Verify_Reset_LoginWithNewPassword()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var admin = await LoginOk(client, AdminUser, AdminPassword);
        var tempName = NewTempUserName();
        var temp = await CreateTempUserAsync(client, admin.AccessToken, tempName);

        try
        {
            // the user picks a question (anonymous list) and sets an answer, proving identity with the password
            var questions = await GetWithToken<List<SecurityQuestionResponse>>(client, "/api/securityquestions", token: null);
            questions.Should().NotBeEmpty();
            var login = await LoginOk(client, tempName, StrongPassword);
            var set = await PutJson(client, "/api/securityquestions/mine",
                new SetSecurityQuestionRequest { QuestionId = questions[0].Id, Answer = "Blue Whale", CurrentPassword = StrongPassword }, login.AccessToken);
            set.StatusCode.Should().Be(HttpStatusCode.OK, await set.Content.ReadAsStringAsync());
            (await MessageOf(set)).Should().Contain(questions[0].QuestionText);
            await Logout(client, login.AccessToken);

            // recovery: start -> question comes back with a one-time token
            var start = await client.PostAsJsonAsync("/api/auth/forgotpassword/start", new ForgotPasswordStartRequest { UserName = tempName });
            start.StatusCode.Should().Be(HttpStatusCode.OK, await start.Content.ReadAsStringAsync());
            var started = (await start.Content.ReadFromJsonAsync<ForgotPasswordStartResponse>())!;
            started.QuestionText.Should().Be(questions[0].QuestionText);

            // wrong answer -> 401, right answer (different case/spacing) -> 200 with a message
            var wrong = await client.PostAsJsonAsync("/api/auth/forgotpassword/verify", new ForgotPasswordVerifyRequest { RequestToken = started.RequestToken, Answer = "Red Fox" });
            wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await ErrorCode(wrong)).Should().Be("SECURITY_ANSWER_INCORRECT");

            var right = await client.PostAsJsonAsync("/api/auth/forgotpassword/verify", new ForgotPasswordVerifyRequest { RequestToken = started.RequestToken, Answer = "  blue   WHALE " });
            right.StatusCode.Should().Be(HttpStatusCode.OK, await right.Content.ReadAsStringAsync());
            (await MessageOf(right)).Should().StartWith("Answer verified.");

            // reset: weak password refused, strong one accepted; then only the new password works
            var weak = await client.PostAsJsonAsync("/api/auth/forgotpassword/reset", new ForgotPasswordResetRequest { RequestToken = started.RequestToken, NewPassword = "short" });
            weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            const string newPassword = "Rec0vered!Passw0rd#3";
            var reset = await client.PostAsJsonAsync("/api/auth/forgotpassword/reset", new ForgotPasswordResetRequest { RequestToken = started.RequestToken, NewPassword = newPassword });
            reset.StatusCode.Should().Be(HttpStatusCode.OK, await reset.Content.ReadAsStringAsync());
            (await MessageOf(reset)).Should().Be("Password has been reset. Log in with the new password.");

            (await LoginFail(client, tempName, StrongPassword)).Should().Be((HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS"));
            var recovered = await LoginOk(client, tempName, newPassword);
            await Logout(client, recovered.AccessToken);

            // the token was single-use
            var again = await client.PostAsJsonAsync("/api/auth/forgotpassword/reset", new ForgotPasswordResetRequest { RequestToken = started.RequestToken, NewPassword = "An0ther!Passw0rd#4" });
            again.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ErrorCode(again)).Should().Be("RESET_REQUEST_INVALID");
        }
        finally
        {
            await Logout(client, admin.AccessToken);
            await DeleteUserAsync(_factory, tempName);
        }
    }

    [Fact]
    public async Task ForgotPassword_UnknownUser_AndUserWithoutQuestion_AnswerIdentically()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var admin = await LoginOk(client, AdminUser, AdminPassword);
        var tempName = NewTempUserName();
        await CreateTempUserAsync(client, admin.AccessToken, tempName);        // exists, no security question

        try
        {
            var unknown = await client.PostAsJsonAsync("/api/auth/forgotpassword/start", new ForgotPasswordStartRequest { UserName = "no-such-user-" + Guid.NewGuid().ToString("N")[..6] });
            var known = await client.PostAsJsonAsync("/api/auth/forgotpassword/start", new ForgotPasswordStartRequest { UserName = tempName });

            unknown.StatusCode.Should().Be(HttpStatusCode.Conflict);
            known.StatusCode.Should().Be(HttpStatusCode.Conflict);

            static async Task<string> Body(HttpResponseMessage r)
            {
                using var doc = await Problem(r);
                var root = doc.RootElement;
                return $"{root.GetProperty("errorCode")}|{root.GetProperty("title")}|{root.GetProperty("detail")}";
            }

            (await Body(unknown)).Should().Be(await Body(known));             // nothing distinguishes the two cases
        }
        finally
        {
            await Logout(client, admin.AccessToken);
            await DeleteUserAsync(_factory, tempName);
        }
    }

    [Fact]
    public async Task AdminConfig_ReadAndValidateUpdates()
    {
        await ResetAdminAsync(_factory);
        using var client = _factory.CreateClient();
        var admin = await LoginOk(client, AdminUser, AdminPassword);

        try
        {
            var config = await GetWithToken<List<SecurityConfigResponse>>(client, "/api/securityconfig", admin.AccessToken);
            var maxSessions = config.Single(c => c.Key == "MAX_ACTIVE_SESSIONS");
            maxSessions.DataType.Should().Be("INT");

            // bad values are refused on the "value" field
            foreach (var bad in new[] { "abc", "0", "" })
            {
                var response = await PutJson(client, "/api/securityconfig/MAX_ACTIVE_SESSIONS", new UpdateSecurityConfigRequest { Value = bad }, admin.AccessToken);
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"value '{bad}'");
                using var body = await Problem(response);
                body.RootElement.GetProperty("errors").TryGetProperty("value", out _).Should().BeTrue();
            }

            // a good value is stored and read back (restore the original so other tests keep their limit)
            var updated = await PutJson(client, "/api/securityconfig/max_active_sessions", new UpdateSecurityConfigRequest { Value = maxSessions.Value }, admin.AccessToken);
            updated.StatusCode.Should().Be(HttpStatusCode.OK);
            (await updated.Content.ReadFromJsonAsync<SecurityConfigResponse>())!.Value.Should().Be(maxSessions.Value);

            (await PutJson(client, "/api/securityconfig/NO_SUCH_KEY", new UpdateSecurityConfigRequest { Value = "1" }, admin.AccessToken))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

            // password policies: list and a rejected edit
            var policies = await GetWithToken<List<PasswordPolicyResponse>>(client, "/api/passwordpolicies", admin.AccessToken);
            var hard = policies.Single(p => p.PolicyName == "HARD");
            var invalid = await PutJson(client, $"/api/passwordpolicies/{hard.Id}", new UpdatePasswordPolicyRequest
            {
                MinLength = 20, MaxLength = 10, RequireNumber = true, PasswordHistoryCount = 5,
            }, admin.AccessToken);
            invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ErrorCode(invalid)).Should().Be("VALIDATION_FAILED");
        }
        finally
        {
            await Logout(client, admin.AccessToken);
        }
    }
}
