using System.Diagnostics;
using System.Text;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Logging;

namespace ST.LiquorTNT.Api.Middleware;

/// <summary>
/// One log entry per API call (paths under /api; Swagger, /health and static files are not logged).
/// Sets up the call trace so the method proxy and the SQL interceptor can record what each step did, then
/// emits the whole call as a single entry: method, path, status, error code, duration, user, IP, and — in
/// DETAIL — the request and response bodies and the full step tree (secrets masked, size capped).
/// Sits outside <see cref="ExceptionMiddleware"/>, so it sees the final status and the error body.
/// Logging never changes what the client receives.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    /// <summary>Set by <see cref="ExceptionMiddleware"/> when it answers with an error; shown in the entry.</summary>
    public const string ErrorCodeItem = "LogErrorCode";

    /// <summary>Set by <see cref="ExceptionMiddleware"/> for a 500: the exception, written inside this call's one entry.</summary>
    public const string ExceptionItem = "LogException";

    private const int MaxBodyLength = 8000;          // what one entry may hold, after masking
    private const int MaxCaptureBytes = 256 * 1024;  // read/kept so the JSON can still be parsed and masked

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly LogModeSwitch _mode;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, LogModeSwitch mode)
    {
        _next = next;
        _logger = logger;
        _mode = mode;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var watch = Stopwatch.StartNew();
        var detail = _mode.IsDetail;
        var session = new CallSession(detail);
        CallTrace.Current = session;

        string requestBody = string.Empty;
        CaptureStream? capture = null;
        if (detail)
        {
            requestBody = await TryReadRequestBodyAsync(context.Request);
            capture = new CaptureStream(context.Response.Body, MaxCaptureBytes);
            context.Response.Body = capture;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            if (capture is not null)
            {
                context.Response.Body = capture.Inner;
            }

            Write(context, watch.ElapsedMilliseconds, session, requestBody, capture);
            CallTrace.Current = null;
        }
    }

    private void Write(HttpContext context, long elapsedMs, CallSession session, string requestBody, CaptureStream? capture)
    {
        var errorCode = context.Items.TryGetValue(ErrorCodeItem, out var code) ? code as string : null;
        var exception = context.Items.TryGetValue(ExceptionItem, out var ex) ? ex as Exception : null;
        var failed = session.FailedAt is not null || exception is not null || errorCode is not null;

        // Order matters: the viewer shows Context top-down. Put what you read first at the top —
        // result, the failure (if any), then the request and response — and the deep step tree last.
        var fields = new Dictionary<string, object?>
        {
            [AppJsonFormatter.LayerProperty] = "Api",
            [AppJsonFormatter.MethodProperty] = ActionName(context),
            ["Result"] = failed ? "FAILED" : "OK",
            ["FailedAt"] = session.FailedAt is null ? null : SafeJson.From(session.FailedAt),
        };

        if (session.Detail)
        {
            fields["Request"] = requestBody;
            fields["Response"] = SafeJson.FromText(capture?.Captured() ?? string.Empty, MaxBodyLength);
        }

        fields["UserId"] = context.User.FindFirst("sub")?.Value;        // known only after authentication ran
        fields["IpAddress"] = context.Connection.RemoteIpAddress?.ToString();

        if (session.Detail)
        {
            fields["Query"] = context.Request.QueryString.Value;
            fields["Steps"] = SafeJson.From(session.Steps);             // the full tree, last
        }

        using (_logger.BeginScope(fields))
        {
            var level = LevelFor(context.Response.StatusCode);
            if (errorCode is null)
            {
                _logger.Log(level, exception, "{HttpMethod} {Path} -> {StatusCode} in {DurationMs} ms",
                    context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, elapsedMs);
            }
            else
            {
                _logger.Log(level, exception, "{HttpMethod} {Path} -> {StatusCode} {ErrorCode} in {DurationMs} ms",
                    context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, errorCode, elapsedMs);
            }
        }
    }

    /// <summary>The controller method that served the call (AuthController.LoginAsync), or HTTP when none matched.</summary>
    private static string ActionName(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>() is { } action
            ? $"{action.ControllerTypeInfo.Name}.{action.MethodInfo.Name}"
            : "HTTP";

    /// <summary>
    /// The request body as it may be logged (already masked). Only a JSON body of reasonable size is read,
    /// with buffering so the controller can read it again; a body that cannot be read is left for the
    /// pipeline, where ExceptionMiddleware answers it.
    /// </summary>
    private static async Task<string> TryReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0 || request.ContentLength > MaxCaptureBytes
            || request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
        {
            return request.ContentLength is > 0
                ? $"<body not logged: {request.ContentType ?? "no content type"}, {request.ContentLength} bytes>"
                : string.Empty;
        }

        try
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return SafeJson.FromText(body, MaxBodyLength);
        }
        catch (Exception ex) when (ex is BadHttpRequestException or IOException or OperationCanceledException)
        {
            return "<body not logged: could not be read>";
        }
    }

    private static LogLevel LevelFor(int status) => status switch
    {
        >= 500 => LogLevel.Error,
        >= 400 => LogLevel.Warning,
        _ => LogLevel.Information,
    };

    /// <summary>Passes every write to the real response stream and keeps a copy of the first bytes.</summary>
    private sealed class CaptureStream : Stream
    {
        private readonly MemoryStream _copy = new();
        private readonly int _limit;

        public CaptureStream(Stream inner, int limit)
        {
            Inner = inner;
            _limit = limit;
        }

        public Stream Inner { get; }

        public string Captured() => Encoding.UTF8.GetString(_copy.GetBuffer(), 0, (int)_copy.Length);

        public override void Write(byte[] buffer, int offset, int count)
        {
            Keep(buffer.AsSpan(offset, count));
            Inner.Write(buffer, offset, count);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Keep(buffer.Span);
            await Inner.WriteAsync(buffer, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush() => Inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => Inner.FlushAsync(cancellationToken);

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        private void Keep(ReadOnlySpan<byte> bytes)
        {
            var room = _limit - (int)_copy.Length;
            if (room > 0)
            {
                _copy.Write(bytes[..Math.Min(room, bytes.Length)]);
            }
        }
    }
}
