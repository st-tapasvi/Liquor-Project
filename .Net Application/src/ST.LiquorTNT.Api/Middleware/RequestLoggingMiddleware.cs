using System.Diagnostics;
using System.Text;
using ST.LiquorTNT.Logging;

namespace ST.LiquorTNT.Api.Middleware;

/// <summary>
/// One log entry per API call (paths under /api; Swagger, /health and static files are not logged).
/// NORMAL: method, path, status, error code, duration, user, IP. DETAIL adds the query string and the
/// request and response bodies (secrets masked, size capped). Sits outside <see cref="ExceptionMiddleware"/>,
/// so it sees the final status and the error body. Logging never changes what the client receives.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    /// <summary>Set by <see cref="ExceptionMiddleware"/> when it answers with an error; shown in the entry.</summary>
    public const string ErrorCodeItem = "LogErrorCode";

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
        var details = new Dictionary<string, object?>
        {
            ["UserId"] = null,
            ["IpAddress"] = context.Connection.RemoteIpAddress?.ToString(),
        };

        if (!_mode.IsDetail)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                Write(context, watch.ElapsedMilliseconds, details);
            }

            return;
        }

        details["Query"] = context.Request.QueryString.Value;
        details["Request"] = await TryReadRequestBodyAsync(context.Request);

        // Written through to the client as it is produced (HasStarted, streaming and aborts behave as
        // without logging); only the first MaxCaptureBytes are kept for the log.
        var capture = new CaptureStream(context.Response.Body, MaxCaptureBytes);
        context.Response.Body = capture;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = capture.Inner;
            details["Response"] = SafeJson.FromText(capture.Captured(), MaxBodyLength);
            Write(context, watch.ElapsedMilliseconds, details);
        }
    }

    private void Write(HttpContext context, long elapsedMs, Dictionary<string, object?> details)
    {
        details[AppJsonFormatter.MethodProperty] = ActionName(context);
        details["UserId"] = context.User.FindFirst("sub")?.Value;       // known only after authentication ran
        var errorCode = context.Items.TryGetValue(ErrorCodeItem, out var code) ? code as string : null;

        using (_logger.BeginScope(details))
        {
            if (errorCode is null)
            {
                _logger.Log(LevelFor(context.Response.StatusCode), "{HttpMethod} {Path} -> {StatusCode} in {DurationMs} ms",
                    context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, elapsedMs);
            }
            else
            {
                _logger.Log(LevelFor(context.Response.StatusCode), "{HttpMethod} {Path} -> {StatusCode} {ErrorCode} in {DurationMs} ms",
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
