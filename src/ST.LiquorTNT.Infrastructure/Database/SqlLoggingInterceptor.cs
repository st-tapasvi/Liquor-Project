using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ST.LiquorTNT.Business.Common;

namespace ST.LiquorTNT.Infrastructure.Database;

/// <summary>
/// In DETAIL log mode, adds every SQL statement an API call runs to the call trace, with the real parameter
/// values put back into the query, so it can be copied straight into a MySQL client. Only genuinely secret
/// values are masked: a stored password hash (starts with <c>PBKDF2.</c>) and a token hash (64 hex characters).
/// User names, ids, dates and statuses are shown as they are. In NORMAL mode nothing is captured here.
/// </summary>
public sealed class SqlLoggingInterceptor : DbCommandInterceptor
{
    private static readonly Regex TokenHash = new("^[0-9a-fA-F]{64}$", RegexOptions.Compiled);

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData e, DbDataReader result)
    {
        Log(command, e.Duration, "reading rows…");      // the row count is filled in when the reader closes
        return base.ReaderExecuted(command, e, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData e, DbDataReader result, CancellationToken ct = default)
    {
        Log(command, e.Duration, "reading rows…");
        return base.ReaderExecutedAsync(command, e, result, ct);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData e, int result)
    {
        Log(command, e.Duration, $"{result} row(s) affected");
        return base.NonQueryExecuted(command, e, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData e, int result, CancellationToken ct = default)
    {
        Log(command, e.Duration, $"{result} row(s) affected");
        return base.NonQueryExecutedAsync(command, e, result, ct);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData e, object? result)
    {
        Log(command, e.Duration, $"returned {result ?? "null"}");
        return base.ScalarExecuted(command, e, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData e, object? result, CancellationToken ct = default)
    {
        Log(command, e.Duration, $"returned {result ?? "null"}");
        return base.ScalarExecutedAsync(command, e, result, ct);
    }

    // A statement that threw (table missing, constraint, timeout …). EF calls this instead of the *Executed
    // callbacks, so without it the one query that broke the call would never appear in the trace.
    public override void CommandFailed(DbCommand command, CommandErrorEventData e)
    {
        Fail(command, e.Exception, e.Duration);
        base.CommandFailed(command, e);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData e, CancellationToken ct = default)
    {
        Fail(command, e.Exception, e.Duration);
        return base.CommandFailedAsync(command, e, ct);
    }

    // When a SELECT's reader closes, EF tells us how many rows the app read; fill it into that SQL node.
    public override InterceptionResult DataReaderClosing(DbCommand command, DataReaderClosingEventData e, InterceptionResult result)
    {
        SetReaderRows(e.ReadCount);
        return base.DataReaderClosing(command, e, result);
    }

    public override ValueTask<InterceptionResult> DataReaderClosingAsync(DbCommand command, DataReaderClosingEventData e, InterceptionResult result)
    {
        SetReaderRows(e.ReadCount);
        return base.DataReaderClosingAsync(command, e, result);
    }

    private static void Log(DbCommand command, TimeSpan duration, string outcome)
    {
        if (CallTrace.Current is not { Detail: true } session)
        {
            return;
        }

        session.LastSql = session.Note("Database", "SQL", output: outcome,
            sql: Inline(command), durationMs: (long)duration.TotalMilliseconds);
    }

    private static void Fail(DbCommand command, Exception exception, TimeSpan duration)
    {
        if (CallTrace.Current is not { Detail: true } session)
        {
            return;
        }

        // The failing query itself (runnable), with the database's own message as its outcome…
        session.LastSql = session.Note("Database", "SQL", output: $"FAILED: {exception.Message}",
            sql: Inline(command), durationMs: (long)duration.TotalMilliseconds);

        // …then pin the failure here (deepest wins) so failedAt names the real spot and the real cause,
        // and mark the still-open steps above it so the tree shows exactly where the call stopped.
        session.Fail("Database", "SQL", ErrorCodes.DatabaseError, exception.Message);
        session.MarkOpenThrew(exception.Message);
    }

    private static void SetReaderRows(int readCount)
    {
        if (CallTrace.Current is { Detail: true, LastSql: { } node } && node.Output as string == "reading rows…")
        {
            node.Output = $"returned {readCount} row(s)";
        }
    }

    private static string Inline(DbCommand command) =>
        BuildRunnableSql(command.CommandText,
            command.Parameters.Cast<DbParameter>().Select(p => (p.ParameterName, (object?)p.Value)).ToList());

    /// <summary>
    /// The command text with each parameter replaced by its value, ready to run in a client. Longest
    /// parameter names go first, so <c>@p1</c> is never matched inside <c>@p10</c>. Public and static so it
    /// can be unit-tested without a live database.
    /// </summary>
    public static string BuildRunnableSql(string commandText, IReadOnlyList<(string Name, object? Value)> parameters)
    {
        var sql = commandText;

        foreach (var (name, value) in parameters.OrderByDescending(p => p.Name.Length))
        {
            sql = sql.Replace("@" + name.TrimStart('@'), Format(value));
        }

        return sql.Trim();
    }

    private static string Format(object? value)
    {
        switch (value)
        {
            case null or DBNull:
                return "NULL";
            case bool b:
                return b ? "1" : "0";
            case byte or sbyte or short or ushort or int or uint or long or ulong or decimal or double or float:
                return Convert.ToString(value, CultureInfo.InvariantCulture)!;
            case DateTime dt:
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
            case DateTimeOffset dto:
                return $"'{dto:yyyy-MM-dd HH:mm:ss}'";
            case byte[]:
                return "'***'";                                  // raw bytes are never a value we need to read
            default:
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return IsSecret(text) ? "'***'" : $"'{Escape(text)}'";
        }
    }

    /// <summary>A stored password hash or a token hash — the only secrets that reach a SQL parameter.</summary>
    private static bool IsSecret(string text) =>
        text.StartsWith("PBKDF2.", StringComparison.OrdinalIgnoreCase) || TokenHash.IsMatch(text);

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("'", "''");
}
