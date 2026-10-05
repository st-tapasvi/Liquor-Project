using Microsoft.Extensions.Logging;

namespace ST.LiquorTNT.Api.Tests.Logging;

/// <summary>One captured entry: the rendered message plus the fields added through a scope (Request, Input …).</summary>
public sealed record LogLine(LogLevel Level, string Message, Exception? Exception, IReadOnlyDictionary<string, object?> Fields)
{
    public string? Field(string name) => Fields.TryGetValue(name, out var value) ? value?.ToString() : null;

    /// <summary>Message and every field, for "is this secret anywhere in the entry" checks.</summary>
    public string All => Message + " " + string.Join(" ", Fields.Select(f => $"{f.Key}={f.Value}"));
}

/// <summary>Keeps every log entry in memory, with the fields of the scopes open at the time.</summary>
public sealed class CapturingLogger : ILogger
{
    private readonly Stack<IEnumerable<KeyValuePair<string, object?>>> _scopes = new();

    public List<LogLine> Lines { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> fields)
        {
            return null;
        }

        _scopes.Push(fields);
        return new Pop(_scopes);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var fields = _scopes.SelectMany(s => s).GroupBy(f => f.Key).ToDictionary(g => g.Key, g => g.First().Value);
        Lines.Add(new LogLine(logLevel, formatter(state, exception), exception, fields));
    }

    private sealed class Pop : IDisposable
    {
        private readonly Stack<IEnumerable<KeyValuePair<string, object?>>> _scopes;

        public Pop(Stack<IEnumerable<KeyValuePair<string, object?>>> scopes) => _scopes = scopes;

        public void Dispose() => _scopes.Pop();
    }
}

public sealed class CapturingLogger<T> : ILogger<T>
{
    public CapturingLogger Inner { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => Inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Inner.Log(logLevel, eventId, state, exception, formatter);
}
