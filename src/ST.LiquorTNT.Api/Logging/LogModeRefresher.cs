using ST.LiquorTNT.Business.Common.Abstractions;
using ST.LiquorTNT.Logging;

namespace ST.LiquorTNT.Api.Logging;

/// <summary>
/// Reads SECURITY_CONFIG.LOG_MODE when the host starts (requests in the first moments are logged as NORMAL)
/// and then every <see cref="Interval"/>, so an administrator's NORMAL / DETAIL change takes effect without a
/// restart. If the database cannot be read, the current mode stays.
/// </summary>
public sealed class LogModeRefresher : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopes;
    private readonly LogModeSwitch _mode;
    private readonly ILogger<LogModeRefresher> _logger;
    private bool _failing;          // the last read failed; the error was already written
    private bool _firstRead = true; // the first successful read is logged: it proves the database is reachable

    public LogModeRefresher(IServiceScopeFactory scopes, LogModeSwitch mode, ILogger<LogModeRefresher> logger)
    {
        _scopes = scopes;
        _mode = mode;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAsync(stoppingToken);

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var settings = await scope.ServiceProvider.GetRequiredService<ISecurityConfigProvider>().GetAsync(ct);

            var changed = _mode.Apply(settings.DetailLogging ? LogMode.Detail : LogMode.Normal);

            // The happy path at start-up is silent (the "API started" entry is enough). Only speak up on
            // recovery after a failure, or when the administrator switches the mode.
            if (_failing)
            {
                _logger.LogWarning("Database reachable again. Log mode is {LogMode}.", _mode.Mode);
            }
            else if (!_firstRead && changed)
            {
                _logger.LogWarning("Log mode changed to {LogMode}.", _mode.Mode);
            }

            _firstRead = false;
            _failing = false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Once per outage, not every 10 seconds.
            if (!_failing)
            {
                _failing = true;
                _logger.LogError(ex, "Database check failed: {Reason} Log mode stays {LogMode}.", ex.Message, _mode.Mode);
            }
        }
    }
}
