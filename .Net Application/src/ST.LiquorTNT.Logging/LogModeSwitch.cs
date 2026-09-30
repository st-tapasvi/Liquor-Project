using Serilog.Core;
using Serilog.Events;

namespace ST.LiquorTNT.Logging;

/// <summary>How much the application writes to its log. Chosen by the administrator (SECURITY_CONFIG.LOG_MODE).</summary>
public enum LogMode
{
    /// <summary>One entry per API call, plus warnings and errors.</summary>
    Normal,

    /// <summary>Everything in Normal plus request/response bodies, every Business method's input, output and
    /// exception, and the SQL the call ran.</summary>
    Detail,
}

/// <summary>
/// The live log mode, shared by the whole process (singleton). Serilog's minimum levels are bound to the
/// switches below, so a change applies to the next log entry without a restart.
/// </summary>
public sealed class LogModeSwitch
{
    /// <summary>EF Core's "Executed DbCommand" category — the SQL text, never parameter values.</summary>
    public const string SqlCategory = "Microsoft.EntityFrameworkCore.Database.Command";

    private volatile bool _detail;

    /// <summary>Level for the application's own entries.</summary>
    public LoggingLevelSwitch Application { get; } = new(LogEventLevel.Information);

    /// <summary>Level for the SQL an API call runs: shown in Detail only.</summary>
    public LoggingLevelSwitch Sql { get; } = new(LogEventLevel.Warning);

    public bool IsDetail => _detail;

    public LogMode Mode => _detail ? LogMode.Detail : LogMode.Normal;

    /// <summary>Switches the mode; returns true when it actually changed.</summary>
    public bool Apply(LogMode mode)
    {
        var detail = mode == LogMode.Detail;
        if (detail == _detail)
        {
            return false;
        }

        Application.MinimumLevel = detail ? LogEventLevel.Debug : LogEventLevel.Information;
        Sql.MinimumLevel = detail ? LogEventLevel.Information : LogEventLevel.Warning;
        _detail = detail;
        return true;
    }
}
