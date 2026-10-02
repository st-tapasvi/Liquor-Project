namespace ST.LiquorTNT.Business.Common;

/// <summary>One node in the tree of what an API call did: a method, a SQL statement, or a sub-step.</summary>
public sealed class CallTraceNode
{
    public int Seq { get; set; }
    public string Layer { get; set; } = string.Empty;      // Api / Business / Domain / Infrastructure / Database
    public string Method { get; set; } = string.Empty;
    public object? Input { get; set; }
    public object? Output { get; set; }                    // what the step produced, or "throw <CODE>" when it failed
    public string? Sql { get; set; }
    public long? DurationMs { get; set; }
    public List<CallTraceNode> Steps { get; } = new();
}

/// <summary>Where a failed call went wrong — the one thing to read first.</summary>
public sealed record CallFailure(string Layer, string Method, string ErrorCode, string Reason);

/// <summary>
/// The tree of one API call, built as it runs. Nesting follows the call stack: a <see cref="Step"/> opened
/// inside another becomes its child. <paramref name="detail"/> off keeps only method names and the failure
/// (for NORMAL mode); on captures inputs, outputs and SQL (for DETAIL). One call is sequential, so the
/// internal stack is safe; parallel fan-out inside a single request is not traced.
/// </summary>
public sealed class CallSession
{
    private readonly Stack<CallTraceNode> _open = new();

    public CallSession(bool detail) => Detail = detail;

    public bool Detail { get; }

    /// <summary>Top-level steps (children of the controller action).</summary>
    public List<CallTraceNode> Steps { get; } = new();

    public CallFailure? FailedAt { get; private set; }

    /// <summary>The last SQL node added, so the interceptor can fill in its row count when the reader closes.</summary>
    public CallTraceNode? LastSql { get; set; }

    private List<CallTraceNode> CurrentChildren => _open.Count > 0 ? _open.Peek().Steps : Steps;

    /// <summary>
    /// Opens a step; steps opened before it closes become its children. Dispose to close it. In NORMAL mode
    /// (not detailed) no node is built — only <see cref="Fail"/> is recorded — so the overhead stays tiny.
    /// </summary>
    public TraceStep Step(string layer, string method, object? input = null)
    {
        if (!Detail)
        {
            return TraceStep.None;
        }

        var node = Add(layer, method);
        node.Input = input;
        _open.Push(node);
        return new TraceStep(this, node);
    }

    /// <summary>A step with no children (a SQL statement, a one-shot check). Ignored in NORMAL mode; returns the node.</summary>
    public CallTraceNode? Note(string layer, string method, object? input = null, object? output = null, string? sql = null, long? durationMs = null)
    {
        if (!Detail)
        {
            return null;
        }

        var node = Add(layer, method);
        node.Input = input;
        node.Output = output;
        node.Sql = sql;
        node.DurationMs = durationMs;
        return node;
    }

    /// <summary>The first failure wins (the deepest, since it is recorded before the exception unwinds).</summary>
    public void Fail(string layer, string method, string errorCode, string reason) =>
        FailedAt ??= new CallFailure(layer, method, errorCode, reason);

    /// <summary>
    /// A step failed deep in the stack: show on each step still open above it that it did not finish, so the
    /// tree points straight at where the call stopped. Called while the stack is intact (before it unwinds),
    /// so every enclosing step is still open. A step that already has an output keeps it.
    /// </summary>
    public void MarkOpenThrew(string reason)
    {
        foreach (var node in _open)
        {
            node.Output ??= $"threw: {reason}";
        }
    }

    internal void Close(CallTraceNode node)
    {
        if (_open.Count > 0 && ReferenceEquals(_open.Peek(), node))
        {
            _open.Pop();
        }
    }

    private CallTraceNode Add(string layer, string method)
    {
        var children = CurrentChildren;
        var node = new CallTraceNode { Seq = children.Count + 1, Layer = layer, Method = method };
        children.Add(node);
        return node;
    }
}

/// <summary>An open step. Set its output/result, then dispose (a <c>using</c> does this) to close it.</summary>
public sealed class TraceStep : IDisposable
{
    /// <summary>The no-op step returned in NORMAL mode: every call on it does nothing.</summary>
    public static readonly TraceStep None = new(null, null);

    private readonly CallSession? _session;
    private readonly CallTraceNode? _node;

    internal TraceStep(CallSession? session, CallTraceNode? node)
    {
        _session = session;
        _node = node;
    }

    public TraceStep Output(object? output)
    {
        if (_node is not null)
        {
            _node.Output = output;
        }

        return this;
    }

    public void Dispose()
    {
        if (_session is not null && _node is not null)
        {
            _session.Close(_node);
        }
    }
}

/// <summary>
/// The trace of the request being handled on this async flow. The Api middleware sets it for a call;
/// the method proxy, the SQL interceptor and the business code add to it. When there is no current trace
/// (unit tests, background work) every call here is a no-op, so the code stays clean and testable.
/// </summary>
public static class CallTrace
{
    private static readonly AsyncLocal<CallSession?> _current = new();

    public static CallSession? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public static TraceStep? Step(string layer, string method, object? input = null) => Current?.Step(layer, method, input);

    public static CallTraceNode? Note(string layer, string method, object? input = null, object? output = null, string? sql = null, long? durationMs = null) =>
        Current?.Note(layer, method, input, output, sql, durationMs);

    public static void Fail(string layer, string method, string errorCode, string reason) =>
        Current?.Fail(layer, method, errorCode, reason);
}
