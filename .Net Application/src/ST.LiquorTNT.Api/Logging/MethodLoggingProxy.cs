using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ST.LiquorTNT.Business.Common.Exceptions;
using ST.LiquorTNT.Logging;

namespace ST.LiquorTNT.Api.Logging;

/// <summary>
/// Wraps a Business service. In DETAIL log mode every call is logged: which method, its input, its output,
/// how long it took, and the exception if it failed. In NORMAL mode the call goes straight through.
/// Inputs and outputs pass through <see cref="SafeJson"/>, so passwords, answers and tokens are masked.
/// </summary>
public class MethodLoggingProxy<TService> : DispatchProxy where TService : class
{
    private static readonly MethodInfo LogTaskOfT =
        typeof(MethodLoggingProxy<TService>).GetMethod(nameof(LogTaskAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    // LogTaskAsync<T> closed over each result type once, not on every call.
    private static readonly ConcurrentDictionary<Type, MethodInfo> LogTaskByResult = new();

    private TService _inner = null!;
    private ILogger _logger = null!;
    private LogModeSwitch _mode = null!;

    public static TService Create(TService inner, ILogger logger, LogModeSwitch mode)
    {
        var proxy = Create<TService, MethodLoggingProxy<TService>>();
        var self = (MethodLoggingProxy<TService>)(object)proxy;
        self._inner = inner;
        self._logger = logger;
        self._mode = mode;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (!_mode.IsDetail)
        {
            return Call(method!, args);
        }

        var name = $"{_inner.GetType().Name}.{method!.Name}";
        var input = Input(method, args);
        var watch = Stopwatch.StartNew();

        object? result;
        try
        {
            result = Call(method, args);
        }
        catch (Exception ex)
        {
            LogFailure(name, input, watch.ElapsedMilliseconds, ex);
            throw;
        }

        if (result is not Task task)
        {
            LogSuccess(name, input, watch.ElapsedMilliseconds, result);
            return result;
        }

        if (!method.ReturnType.IsGenericType)
        {
            return LogVoidTaskAsync(task, name, input, watch);
        }

        var logTask = LogTaskByResult.GetOrAdd(method.ReturnType.GetGenericArguments()[0], t => LogTaskOfT.MakeGenericMethod(t));
        return logTask.Invoke(this, new object[] { task, name, input, watch });
    }

    private async Task<T> LogTaskAsync<T>(Task<T> task, string name, string input, Stopwatch watch)
    {
        try
        {
            var result = await task;
            LogSuccess(name, input, watch.ElapsedMilliseconds, result);
            return result;
        }
        catch (Exception ex)
        {
            LogFailure(name, input, watch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    private async Task LogVoidTaskAsync(Task task, string name, string input, Stopwatch watch)
    {
        try
        {
            await task;
            LogSuccess(name, input, watch.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            LogFailure(name, input, watch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    /// <summary>Calls the real service; reflection's wrapper exception is removed so callers see the original.</summary>
    private object? Call(MethodInfo method, object?[]? args)
    {
        try
        {
            return method.Invoke(_inner, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;      // unreachable
        }
    }

    /// <summary>Arguments by parameter name; the CancellationToken is left out.</summary>
    private static string Input(MethodInfo method, object?[]? args)
    {
        var values = new Dictionary<string, object?>();
        var parameters = method.GetParameters();

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType != typeof(CancellationToken))
            {
                values[parameters[i].Name!] = args?[i];
            }
        }

        return SafeJson.From(values);
    }

    // Method, Input and Output go in as their own fields (the formatter puts Method at the top and
    // Input / Output into Context as JSON), not into the message.

    private void LogSuccess(string name, string input, long elapsedMs, object? output)
    {
        using (_logger.BeginScope(Fields(name, input, SafeJson.From(output))))
        {
            _logger.LogInformation("OK in {DurationMs} ms", elapsedMs);
        }
    }

    private void LogFailure(string name, string input, long elapsedMs, Exception ex)
    {
        using (_logger.BeginScope(Fields(name, input, output: null)))
        {
            if (ex is AppException app)
            {
                // an expected business refusal (wrong password, not found …): no stack trace needed
                _logger.LogWarning("Refused: {ErrorCode} {Reason} ({DurationMs} ms)", app.ErrorCode, app.Detail ?? app.Title, elapsedMs);
                return;
            }

            _logger.LogError(ex, "Failed: {Reason} ({DurationMs} ms)", ex.Message, elapsedMs);
        }
    }

    private static Dictionary<string, object?> Fields(string name, string input, string? output) => new()
    {
        [AppJsonFormatter.MethodProperty] = name,
        ["Input"] = input,
        ["Output"] = output,
    };
}
