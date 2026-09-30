using FluentAssertions;
using Microsoft.Extensions.Logging;
using ST.LiquorTNT.Api.Logging;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.Auth;
using ST.LiquorTNT.Logging;
using Xunit;

namespace ST.LiquorTNT.Api.Tests.Logging;

public interface ISampleService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);

    Task FailAsync(int id, CancellationToken ct);

    Task RefuseAsync(CancellationToken ct);

    int Add(int a, int b);
}

public sealed class SampleService : ISampleService
{
    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct) =>
        Task.FromResult(new LoginResponse { AccessToken = "eyJ.secret.token", User = new CurrentUserResponse { UserName = request.UserName } });

    public async Task FailAsync(int id, CancellationToken ct)
    {
        await Task.Yield();
        throw new InvalidOperationException($"boom {id}");
    }

    public Task RefuseAsync(CancellationToken ct) =>
        throw new UnauthorizedException(ErrorCodes.InvalidCredentials, "User name or password is incorrect.");

    public int Add(int a, int b) => a + b;
}

/// <summary>DETAIL logs each Business call's input, output and exception; NORMAL logs nothing; results never change.</summary>
public sealed class MethodLoggingProxyTests
{
    private readonly CapturingLogger _log = new();
    private readonly LogModeSwitch _mode = new();
    private readonly ISampleService _service;

    public MethodLoggingProxyTests() => _service = MethodLoggingProxy<ISampleService>.Create(new SampleService(), _log, _mode);

    [Fact]
    public async Task Normal_NothingLogged_ResultUnchanged()
    {
        var response = await _service.LoginAsync(new LoginRequest { UserName = "ravi", Password = "pw" }, CancellationToken.None);

        response.User.UserName.Should().Be("ravi");
        _log.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Detail_LogsMethodInputOutput_SecretsMasked()
    {
        _mode.Apply(LogMode.Detail);

        var response = await _service.LoginAsync(new LoginRequest { UserName = "ravi", Password = "S3cret!" }, CancellationToken.None);

        response.AccessToken.Should().Be("eyJ.secret.token");           // the caller still gets the real value
        var line = _log.Lines.Should().ContainSingle().Subject;
        line.Level.Should().Be(LogLevel.Information);
        line.Field("Method").Should().Be("SampleService.LoginAsync");
        line.Message.Should().StartWith("OK in");
        line.Field("Input").Should().Contain("\"userName\":\"ravi\"").And.Contain("\"password\":\"***\"").And.NotContain("\"ct\"");
        line.Field("Output").Should().Contain("\"accessToken\":\"***\"");
        line.All.Should().NotContain("S3cret!").And.NotContain("eyJ.secret.token");
    }

    [Fact]
    public async Task Detail_UnexpectedException_LoggedAsErrorWithInput_AndRethrown()
    {
        _mode.Apply(LogMode.Detail);

        await _service.Invoking(s => s.FailAsync(7, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("boom 7");

        var line = _log.Lines.Should().ContainSingle().Subject;
        line.Level.Should().Be(LogLevel.Error);
        line.Exception.Should().BeOfType<InvalidOperationException>();
        line.Field("Method").Should().Be("SampleService.FailAsync");
        line.Message.Should().StartWith("Failed: boom 7");
        line.Field("Input").Should().Be("{\"id\":7}");
    }

    [Fact]
    public async Task Detail_BusinessRefusal_WarningWithErrorCode_OriginalExceptionType()
    {
        _mode.Apply(LogMode.Detail);

        await _service.Invoking(s => s.RefuseAsync(CancellationToken.None)).Should().ThrowAsync<UnauthorizedException>();

        var line = _log.Lines.Should().ContainSingle().Subject;
        line.Level.Should().Be(LogLevel.Warning);
        line.Message.Should().Contain("INVALID_CREDENTIALS");
    }

    [Fact]
    public void Detail_SyncMethod_LoggedToo()
    {
        _mode.Apply(LogMode.Detail);

        _service.Add(2, 3).Should().Be(5);

        _log.Lines.Should().ContainSingle(l => l.Field("Input") == "{\"a\":2,\"b\":3}" && l.Field("Output") == "5");
    }

    [Fact]
    public async Task SwitchBackToNormal_StopsLogging()
    {
        _mode.Apply(LogMode.Detail);
        _mode.Apply(LogMode.Normal);

        await _service.LoginAsync(new LoginRequest { UserName = "ravi", Password = "pw" }, CancellationToken.None);

        _log.Lines.Should().BeEmpty();
    }
}
