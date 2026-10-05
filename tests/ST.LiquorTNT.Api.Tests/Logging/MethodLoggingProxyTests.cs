using FluentAssertions;
using ST.LiquorTNT.Api.Logging;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Contracts.Auth;
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

/// <summary>
/// The proxy records each Business call as a node in the current trace (input, output, and where it failed);
/// with no trace (NORMAL, or no request) it just runs the call. Results are never changed.
/// </summary>
public sealed class MethodLoggingProxyTests : IDisposable
{
    private readonly ISampleService _service = MethodLoggingProxy<ISampleService>.Create(new SampleService());

    public void Dispose() => CallTrace.Current = null;

    private static CallSession StartDetailTrace()
    {
        var session = new CallSession(detail: true);
        CallTrace.Current = session;
        return session;
    }

    [Fact]
    public async Task NoTrace_NothingRecorded_ResultUnchanged()
    {
        var response = await _service.LoginAsync(new LoginRequest { UserName = "ravi", Password = "pw" }, CancellationToken.None);

        response.User.UserName.Should().Be("ravi");
        CallTrace.Current.Should().BeNull();
    }

    [Fact]
    public async Task Detail_RecordsMethodInputOutput()
    {
        var session = StartDetailTrace();

        var response = await _service.LoginAsync(new LoginRequest { UserName = "ravi", Password = "S3cret!" }, CancellationToken.None);

        response.AccessToken.Should().Be("eyJ.secret.token");           // the caller still gets the real value
        var node = session.Steps.Should().ContainSingle().Subject;
        node.Layer.Should().Be("Business");
        node.Method.Should().Be("SampleService.LoginAsync");
        node.Output.Should().BeOfType<LoginResponse>();      // the return value; secrets masked later by SafeJson
    }

    [Fact]
    public async Task Detail_BusinessRefusal_RecordsFailedAt_AndRethrows()
    {
        var session = StartDetailTrace();

        await _service.Invoking(s => s.RefuseAsync(CancellationToken.None)).Should().ThrowAsync<UnauthorizedException>();

        session.FailedAt!.Method.Should().Be("SampleService.RefuseAsync");
        session.FailedAt.ErrorCode.Should().Be(ErrorCodes.InvalidCredentials);
        session.Steps.Single().Output.Should().Be("throw INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Detail_UnexpectedException_RecordedAndRethrown()
    {
        var session = StartDetailTrace();

        await _service.Invoking(s => s.FailAsync(7, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("boom 7");

        session.FailedAt!.Method.Should().Be("SampleService.FailAsync");
        session.Steps.Single().Output.Should().Be("throw InvalidOperationException");
    }

    [Fact]
    public void Detail_SyncMethod_Recorded()
    {
        var session = StartDetailTrace();

        _service.Add(2, 3).Should().Be(5);

        var node = session.Steps.Single();
        node.Method.Should().Be("SampleService.Add");
        node.Output.Should().Be(5);
    }

    [Fact]
    public async Task Normal_OnlyFailedAtKept_NoStepTree()
    {
        var session = new CallSession(detail: false);
        CallTrace.Current = session;

        await _service.Invoking(s => s.RefuseAsync(CancellationToken.None)).Should().ThrowAsync<UnauthorizedException>();

        session.Steps.Should().BeEmpty();                       // no tree built in NORMAL
        session.FailedAt!.ErrorCode.Should().Be(ErrorCodes.InvalidCredentials);   // but the failure is still known
    }
}
