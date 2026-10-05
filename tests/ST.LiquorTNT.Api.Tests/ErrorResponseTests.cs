using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using static ST.LiquorTNT.Api.Tests.ApiTestHelpers;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>Mistakes a client makes before any controller runs still come back as readable ProblemDetails.</summary>
[Collection(ApiCollection.Name)]
public sealed class ErrorResponseTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public ErrorResponseTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Theory]
    [InlineData("/api/no/such/api")]
    [InlineData("/api/auth/change-password")]      // old hyphenated route is gone
    [InlineData("/api/users/abc")]                 // id must be a number
    public async Task UnknownAddress_404_WithBody(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await ErrorCode(response)).Should().Be("ENDPOINT_NOT_FOUND");
    }

    [Fact]
    public async Task WrongMethod_405_WithBody()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/login");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await ErrorCode(response)).Should().Be("METHOD_NOT_ALLOWED");
    }

    [Fact]
    public async Task BodyNotJson_415_WithBody()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/login", new StringContent("userName=admin", Encoding.UTF8, "text/plain"));

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        (await ErrorCode(response)).Should().Be("UNSUPPORTED_MEDIA_TYPE");
    }

    [Fact]
    public async Task JwtPastItsHardLimit_401SessionExpired_SoTheClientShowsThePasswordPopup()
    {
        using var client = _factory.CreateClient();
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"]!));
        var expired = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"],
            claims: new[] { new Claim("sub", "1"), new Claim("unique_name", "admin") },
            notBefore: DateTime.UtcNow.AddHours(-25), expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));

        var response = await client.SendAsync(WithToken(HttpMethod.Get, "/api/auth/me", expired));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCode(response)).Should().Be("SESSION_EXPIRED");
    }

    [Theory]
    [InlineData("")]            // JSON content type, but no body at all
    [InlineData("{ broken")]    // malformed JSON
    public async Task EmptyOrBrokenJson_400_WithBody(string json)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/login", new StringContent(json, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
        (await ErrorCode(response)).Should().Be("VALIDATION_FAILED");
    }
}
