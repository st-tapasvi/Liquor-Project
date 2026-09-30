using Serilog.Context;
using ST.LiquorTNT.Logging;

namespace ST.LiquorTNT.Api.Middleware;

/// <summary>
/// Gives every request a correlation id, echoes it in the response header and pushes it into the log
/// context. It is the reference that joins browser, API log and database record.
/// </summary>
public sealed class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(LogFields.CorrelationId, correlationId))
        {
            await _next(context);
        }
    }
}
