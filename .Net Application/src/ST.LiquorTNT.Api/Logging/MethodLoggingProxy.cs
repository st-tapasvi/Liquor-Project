using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ST.LiquorTNT.Business.Common;
using ST.LiquorTNT.Business.Common.Exceptions;

namespace ST.LiquorTNT.Api.Logging;

/// <summary>
/// Wraps a Business service. Each call becomes a node in the current <see cref="CallTrace"/>: which method,
/// its input, its output, and — if it threw a business error — where it failed. SQL run inside the call and
/// any inner steps nest under this node. When there is no active trace (no request, unit tests) the call
/// goes straight through. It writes nothing to the log itself; the request middleware emits the whole tree once.
/// </summary>
public class MethodLoggingProxy<TService> : DispatchProxy where TService : class
{
    private static readonly MethodInfo TraceTaskOfT =
        typeof(MethodLoggingProxy<TService>).GetMethod(nameof(TraceTaskAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> TraceTaskByResult = new();

    private TService _inner = null!;

    public static TService Create(TService inner)
    {
        var proxy = Create<TService, MethodLoggingProxy<TService>>();
        ((MethodLoggingProxy<TService>)(object)proxy)._inner = inner;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        var session = CallTrace.Current;
        if (session is null)
        {
            return Call(method!, args);
        }

        var name = $"{_inner.GetType().Name}.{method!.Name}";
        var step = session.Step("Business", name, session.Detail ? Input(method, args) : null);

        object? result;
        try
        {
            result = Call(method, args);
        }
        catch (Exception ex)
        {
            Failed(session, name, step, ex);
            step.Dispose();
            throw;
        }

        if (result is not Task task)
        {
            step.Output(result);
            step.Dispose();
            return result;
        }

        if (!method.ReturnType.IsGenericType)
        {
            return TraceVoidTaskAsync(task, session, name, step);
        }

        var traceTask = TraceTaskByResult.GetOrAdd(method.ReturnType.GetGenericArguments()[0], t => TraceTaskOfT.MakeGenericMethod(t));
        return traceTask.Invoke(this, new object[] { task, session, name, step })!;
    }

    private async Task<T> TraceTaskAsync<T>(Task<T> task, CallSession session, string name, TraceStep step)
    {
        try
        {
            var result = await task;
            step.Output(result);
            return result;
        }
        catch (Exception ex)
        {
            Failed(session, name, step, ex);
            throw;
        }
        finally
        {
            step.Dispose();
        }
    }

    private async Task TraceVoidTaskAsync(Task task, CallSession session, string name, TraceStep step)
    {
        try
        {
            await task;
            step.Output("done");
        }
        catch (Exception ex)
        {
            Failed(session, name, step, ex);
            throw;
        }
        finally
        {
            step.Dispose();
        }
    }

    /// <summary>An expected business refusal records where it failed; an unexpected one is just marked on the node.</summary>
    private static void Failed(CallSession session, string name, TraceStep step, Exception ex)
    {
        if (ex is AppException app)
        {
            session.Fail("Business", name, app.ErrorCode, app.Detail ?? app.Title);
            step.Output($"throw {app.ErrorCode}");
        }
        else
        {
            // A database fault (missing table, timeout, constraint) is a DATABASE_ERROR, as ExceptionMiddleware
            // answers it — not "unexpected". The SQL interceptor usually pins this first (deepest wins); this is
            // the fallback for a DB fault that did not come through a command (e.g. on connecting).
            var code = IsDatabaseFailure(ex) ? ErrorCodes.DatabaseError : ErrorCodes.Unexpected;
            session.Fail("Business", name, code, ex.Message);
            step.Output($"throw {ex.GetType().Name}");
        }
    }

    /// <summary>True if this exception, or any it wraps, is a database error (System.Data.Common.DbException).</summary>
    private static bool IsDatabaseFailure(Exception ex)
    {
        for (var current = (Exception?)ex; current is not null; current = current.InnerException)
        {
            if (current is DbException)
            {
                return true;
            }
        }

        return false;
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

    /// <summary>Arguments by parameter name; the CancellationToken is left out. Masked when the tree is serialised.</summary>
    private static Dictionary<string, object?> Input(MethodInfo method, object?[]? args)
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

        return values;
    }
}
