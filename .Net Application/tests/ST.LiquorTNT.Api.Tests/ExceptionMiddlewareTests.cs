using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using ST.LiquorTNT.Api.Middleware;
using Xunit;

namespace ST.LiquorTNT.Api.Tests;

/// <summary>Every error leaves the API with a ProblemDetails body — also the ones the framework answers on its own.</summary>
public sealed class ExceptionMiddlewareTests
{
    private static async Task<(int Status, JsonElement Body)> Run(RequestDelegate next, ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/users/5/unlock";
        context.Response.Body = new MemoryStream();
        if (user is not null)
        {
            context.User = user;
        }

        await new ExceptionMiddleware(next).InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, doc.RootElement.Clone());
    }

    [Theory]
    [InlineData(401, "UNAUTHENTICATED")]
    [InlineData(403, "FORBIDDEN")]
    [InlineData(404, "ENDPOINT_NOT_FOUND")]
    [InlineData(405, "METHOD_NOT_ALLOWED")]
    [InlineData(415, "UNSUPPORTED_MEDIA_TYPE")]
    [InlineData(409, "REQUEST_REFUSED")]
    [InlineData(502, "UNEXPECTED_ERROR")]
    public async Task BareErrorStatus_GetsProblemDetailsBody(int status, string errorCode)
    {
        var (actual, body) = await Run(ctx => { ctx.Response.StatusCode = status; return Task.CompletedTask; });

        actual.Should().Be(status);
        body.GetProperty("errorCode").GetString().Should().Be(errorCode);
        body.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("detail").GetString().Should().Contain("/api/users/5/unlock");
    }

    [Fact]
    public async Task ErrorBody_FixedKeysInOrder()
    {
        var (_, body) = await Run(ctx => { ctx.Response.StatusCode = 404; return Task.CompletedTask; });

        body.EnumerateObject().Select(p => p.Name).Should().Equal("type", "status", "errorCode", "title", "detail", "instance", "correlationId");
        body.GetProperty("type").GetString().Should().Be("https://errors.stliquortnt.local/endpoint_not_found");
    }

    [Fact]
    public async Task Forbidden_NamesTheCallersRole()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("role_id", "4") }, "test"));

        var (_, body) = await Run(ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; }, user);

        body.GetProperty("detail").GetString().Should().Contain("role_id: '4'");
    }

    [Fact]
    public async Task UnexpectedException_ShowsMessageChainTypeAndStack()
    {
        var (status, body) = await Run(_ => throw new InvalidOperationException("outer", new TimeoutException("inner cause")));

        status.Should().Be(500);
        body.GetProperty("errorCode").GetString().Should().Be("UNEXPECTED_ERROR");
        body.GetProperty("detail").GetString().Should().Be("InvalidOperationException: outer --> TimeoutException: inner cause");
        body.GetProperty("exceptionType").GetString().Should().Be(typeof(InvalidOperationException).FullName);
        body.GetProperty("stackTrace").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StackTrace_StartsWithTheInnermostCause()
    {
        static void Inner() => throw new TimeoutException("db timeout");
        static void Outer()
        {
            try { Inner(); }
            catch (Exception ex) { throw new InvalidOperationException("outer", ex); }
        }

        var (_, body) = await Run(_ => { Outer(); return Task.CompletedTask; });

        var frames = body.GetProperty("stackTrace").EnumerateArray().Select(f => f.GetString()!).ToList();
        frames[0].Should().Be($"[{typeof(TimeoutException).FullName}]");
        frames.Should().Contain($"[{typeof(InvalidOperationException).FullName}]");
        frames.Should().NotContain(f => f.StartsWith("---"));
    }

    [Fact]
    public async Task UnreadableRequest_UsesKestrelStatus_NotA500()
    {
        var (status, body) = await Run(_ => throw new BadHttpRequestException("Request body too large.", 413));

        status.Should().Be(413);
        body.GetProperty("errorCode").GetString().Should().Be("REQUEST_INVALID");
        body.GetProperty("detail").GetString().Should().Contain("Request body too large.");
    }

    [Fact]
    public async Task ClientCancelled_WritesNothing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = new DefaultHttpContext { RequestAborted = cts.Token, Response = { Body = new MemoryStream() } };

        await new ExceptionMiddleware(_ => throw new OperationCanceledException()).InvokeAsync(context);

        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task SuccessfulResponse_IsLeftAlone()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await new ExceptionMiddleware(_ => Task.CompletedTask).InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
        context.Response.Body.Length.Should().Be(0);
    }
}
