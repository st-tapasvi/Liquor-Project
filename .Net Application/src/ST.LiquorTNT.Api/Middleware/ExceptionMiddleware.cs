using System.Data.Common;
using System.Text;
using System.Text.Json;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;

namespace ST.LiquorTNT.Api.Middleware;

/// <summary>
/// The only place where an error becomes an HTTP response. Controllers never catch to shape a response.
/// Output is RFC 7807 ProblemDetails plus errorCode and correlationId — for every error, including the
/// ones the framework answers without a body (no matching route, wrong method, not JSON, role refused).
/// Unexpected exceptions carry their message chain, type and stack frames in every environment, so a
/// response alone is enough to start troubleshooting (owner decision).
/// </summary>
public sealed class ExceptionMiddleware
{
    private const int MaxStackFrames = 15;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // An error status with nothing written yet: the framework refused the request on its own.
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted)
            {
                var (code, title, detail) = DescribeBareStatus(context);
                await WriteAsync(context, context.Response.StatusCode, title, detail, code, errors: null, exception: null);
            }
        }
        catch (AppException ex)
        {
            // An expected refusal: RequestLoggingMiddleware writes it as the call's one entry (with errorCode).
            await WriteAsync(context, ex.StatusCode, ex.Title, ex.Detail, ex.ErrorCode,
                (ex as ValidationException)?.Errors, exception: null);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away (tab closed, timeout). Not a server fault, and nobody is left to read a body.
            _logger.LogInformation("{HttpMethod} {Path} cancelled by the client", context.Request.Method, context.Request.Path.Value);
        }
        catch (BadHttpRequestException ex)
        {
            // Kestrel could not read the request (body too large, broken chunking …): the caller's fault, not a 500.
            await WriteAsync(context, ex.StatusCode, "The request could not be read.",
                $"{context.Request.Method} {context.Request.Path}: {ex.Message}", ErrorCodes.RequestInvalid,
                errors: null, exception: null);
        }
        catch (Exception ex) when (ex is DbException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            _logger.LogError(ex, "Database error in {HttpMethod} {Path}: {Reason}", context.Request.Method, context.Request.Path.Value, ex.Message);

            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "The database could not serve this request.", Describe(ex), ErrorCodes.DatabaseError,
                errors: null, exception: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in {HttpMethod} {Path}: {Reason}", context.Request.Method, context.Request.Path.Value, ex.Message);

            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.", Describe(ex), ErrorCodes.Unexpected,
                errors: null, exception: ex);
        }
    }

    /// <summary>What went wrong when the framework set an error status without a body.</summary>
    private static (string Code, string Title, string Detail) DescribeBareStatus(HttpContext context)
    {
        var call = $"{context.Request.Method} {context.Request.Path}";

        return context.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => (ErrorCodes.Unauthenticated,
                "Authentication is required.", $"{call} needs a valid bearer token."),
            StatusCodes.Status403Forbidden => (ErrorCodes.Forbidden,
                "You are not allowed to use this API.",
                $"{call} needs a role the caller does not have (caller role_id: '{context.User.FindFirst("role_id")?.Value ?? "none"}')."),
            StatusCodes.Status404NotFound => (ErrorCodes.EndpointNotFound,
                "No API exists at this address.",
                $"Nothing answers {call}. Check the URL: routes are lowercase without '-', e.g. /api/auth/changepassword."),
            StatusCodes.Status405MethodNotAllowed => (ErrorCodes.MethodNotAllowed,
                "This HTTP method is not supported here.",
                $"{context.Request.Path} exists, but does not accept {context.Request.Method}."),
            StatusCodes.Status415UnsupportedMediaType => (ErrorCodes.UnsupportedMediaType,
                "The request body must be JSON.", $"{call}: send the body with the header Content-Type: application/json."),
            < 500 => (ErrorCodes.RequestRefused, "The request was refused.", $"{call} ended with status {context.Response.StatusCode}."),
            _ => (ErrorCodes.Unexpected, "The request could not be completed.", $"{call} ended with status {context.Response.StatusCode}."),
        };
    }

    /// <summary>Flattens the inner-exception chain - the real cause is usually the innermost one.</summary>
    private static string Describe(Exception ex)
    {
        var builder = new StringBuilder();
        var current = (Exception?)ex;
        var level = 0;

        while (current is not null)
        {
            builder.Append(level == 0 ? string.Empty : " --> ")
                   .Append(current.GetType().Name)
                   .Append(": ")
                   .Append(current.Message);

            current = current.InnerException;
            level++;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Frames of every exception in the chain, innermost (the real cause) first, each headed by its type
    /// and capped at <see cref="MaxStackFrames"/>. Runtime separator lines are dropped.
    /// </summary>
    private static List<string> StackFrames(Exception ex)
    {
        var chain = new List<Exception>();
        for (var current = (Exception?)ex; current is not null; current = current.InnerException)
        {
            chain.Insert(0, current);
        }

        var lines = new List<string>();
        foreach (var item in chain)
        {
            lines.Add($"[{item.GetType().FullName}]");
            lines.AddRange((item.StackTrace ?? string.Empty)
                .Split('\n')
                .Select(line => line.TrimEnd('\r').Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("---", StringComparison.Ordinal))
                .Take(MaxStackFrames));
        }

        return lines;
    }

    private static async Task WriteAsync(
        HttpContext context,
        int status,
        string title,
        string? detail,
        string errorCode,
        IReadOnlyDictionary<string, string[]>? errors,
        Exception? exception)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        // No Response.Clear(): it would drop the X-Correlation-Id and CORS headers already set upstream.
        context.Response.StatusCode = status;
        context.Items[RequestLoggingMiddleware.ErrorCodeItem] = errorCode;
        context.Response.ContentType = "application/problem+json";

        var correlationId = context.Items.TryGetValue(CorrelationMiddleware.ItemKey, out var value)
            ? value as string
            : null;

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://errors.stliquortnt.local/{errorCode.ToLowerInvariant()}",
            ["title"] = title,
            ["status"] = status,
            ["detail"] = string.IsNullOrWhiteSpace(detail) ? null : detail,
            ["instance"] = context.Request.Path.Value,
            ["errorCode"] = errorCode,
            ["correlationId"] = correlationId,
        };

        if (errors is { Count: > 0 })
        {
            payload["errors"] = errors;
        }

        if (exception is not null)
        {
            payload["exceptionType"] = exception.GetType().FullName;
            payload["stackTrace"] = StackFrames(exception);
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
