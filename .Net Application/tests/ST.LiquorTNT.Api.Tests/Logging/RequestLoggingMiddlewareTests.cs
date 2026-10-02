using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ST.LiquorTNT.Api.Middleware;
using ST.LiquorTNT.Logging;
using Xunit;

namespace ST.LiquorTNT.Api.Tests.Logging;

public sealed class RequestLoggingMiddlewareTests
{
    private readonly CapturingLogger<RequestLoggingMiddleware> _log = new();
    private readonly LogModeSwitch _mode = new();

    private const string RequestJson = "{\"userName\":\"admin\",\"password\":\"Admin@123\"}";
    private const string ResponseJson = "{\"accessToken\":\"eyJ.a.b\",\"expiresAt\":\"2026-09-29T10:00:00\"}";

    private async Task<(string ClientBody, string ControllerSaw)> Run(
        int status = 200, string requestJson = RequestJson, string path = "/api/auth/login", string? errorCode = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestJson));
        context.Request.ContentLength = Encoding.UTF8.GetByteCount(requestJson);
        var client = new MemoryStream();
        context.Response.Body = client;
        var controllerSaw = string.Empty;

        var middleware = new RequestLoggingMiddleware(async ctx =>
        {
            controllerSaw = await new StreamReader(ctx.Request.Body).ReadToEndAsync();   // the body is still readable
            ctx.Response.StatusCode = status;
            if (errorCode is not null)
            {
                ctx.Items[RequestLoggingMiddleware.ErrorCodeItem] = errorCode;             // as ExceptionMiddleware does
            }
            await ctx.Response.WriteAsync(ResponseJson);
        }, _log, _mode);

        await middleware.InvokeAsync(context);
        return (Encoding.UTF8.GetString(client.ToArray()), controllerSaw);
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/health")]
    public async Task NotAnApiCall_NotLogged_InEitherMode(string path)
    {
        await Run(path: path);
        _mode.Apply(LogMode.Detail);
        await Run(path: path);

        _log.Inner.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Normal_OneEntry_NoBodies()
    {
        var (clientBody, _) = await Run();

        clientBody.Should().Be(ResponseJson);
        var line = _log.Inner.Lines.Should().ContainSingle().Subject;
        line.Message.Should().StartWith("POST /api/auth/login -> 200 in");
        line.Field("Method").Should().Be("HTTP");                 // no endpoint in this unit test
        line.Fields.Should().NotContainKey("Request").And.NotContainKey("Response");
        line.All.Should().NotContain("Admin@123");
    }

    [Fact]
    public async Task ErrorCode_IsPartOfTheMessage()
    {
        await Run(status: 401, errorCode: "INVALID_CREDENTIALS");

        _log.Inner.Lines.Single().Message.Should().StartWith("POST /api/auth/login -> 401 INVALID_CREDENTIALS in");
    }

    [Fact]
    public async Task Detail_OneEntry_WithRequestResponseAndSteps()
    {
        _mode.Apply(LogMode.Detail);

        var (clientBody, controllerSaw) = await Run();

        clientBody.Should().Be(ResponseJson);                 // client gets the real token
        controllerSaw.Should().Be(RequestJson);               // controller gets the real password

        var line = _log.Inner.Lines.Should().ContainSingle().Subject;
        line.Field("Layer").Should().Be("Api");
        line.Field("Request").Should().Be("{\"userName\":\"admin\",\"password\":\"***\"}");
        line.Field("Response").Should().Contain("2026-09-29T10:00:00").And.Contain("\"accessToken\":\"***\"");
        line.Fields.Should().ContainKey("Steps");             // the tree field is present (empty here — no proxy in this unit test)
        line.All.Should().NotContain("Admin@123").And.NotContain("eyJ.a.b");
    }

    [Theory]
    [InlineData(200, LogLevel.Information, false)]
    [InlineData(401, LogLevel.Warning, false)]
    [InlineData(500, LogLevel.Error, false)]
    [InlineData(200, LogLevel.Information, true)]
    [InlineData(401, LogLevel.Warning, true)]
    [InlineData(500, LogLevel.Error, true)]
    public async Task LevelFollowsStatus_BothModes(int status, LogLevel level, bool detail)
    {
        _mode.Apply(detail ? LogMode.Detail : LogMode.Normal);

        await Run(status);

        _log.Inner.Lines.Last().Level.Should().Be(level);       // the final entry carries the status
    }

    [Theory]
    [InlineData("{\"userName\":\"admin\",\"password\":\"Admin@123\",}")]
    [InlineData("{\"password\":\"Admin@123\",\"password\":\"x\"}")]
    public async Task Detail_MalformedBody_NotLogged_RequestStillServed(string badJson)
    {
        _mode.Apply(LogMode.Detail);

        var (clientBody, controllerSaw) = await Run(requestJson: badJson);

        controllerSaw.Should().Be(badJson);
        clientBody.Should().Be(ResponseJson);
        _log.Inner.Lines.Last().Field("Request").Should().StartWith("<body not logged");
        _log.Inner.Lines.Should().NotContain(l => l.All.Contains("Admin@123"));
    }
}
