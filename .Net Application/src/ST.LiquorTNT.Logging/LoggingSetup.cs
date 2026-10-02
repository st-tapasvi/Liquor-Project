using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace ST.LiquorTNT.Logging;

public static class LoggingSetup
{
    private const string StartupMethod = "Startup";

    /// <summary>
    /// Wires Serilog in two stages. A start-up logger writes to the same sinks straight away, so a failure
    /// while the API is still being built (bad configuration, DI error) is in the log file too. When the host
    /// is built, the full logger replaces it: levels bound to <see cref="LogModeSwitch"/> (NORMAL / DETAIL live),
    /// the framework reduced to warnings and errors, except start-up messages and the SQL of an API call in DETAIL.
    /// </summary>
    public static IHostBuilder UseAppLogging(this IHostBuilder host, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateBootstrapLogger();

        return host
            .ConfigureServices(services => services.AddSingleton<LogModeSwitch>())
            .UseSerilog((context, services, logger) =>
            {
                var mode = services.GetRequiredService<LogModeSwitch>();
                var fromFramework = Matching.FromSource("Microsoft");
                var fromHostLifetime = Matching.FromSource("Microsoft.Hosting.Lifetime");

                logger
                    .MinimumLevel.ControlledBy(mode.Application)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    // The framework's own "Now listening / Application started / Content root" lines are
                    // silenced; we emit one combined "API started" entry instead.
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
                    // EF Core's own logs are fully silenced (even its error "Failed executing DbCommand" and
                    // "exception while iterating", which would otherwise add 2 extra entries per failed call):
                    // the runnable SQL comes from SqlLoggingInterceptor and the failure from the one call entry.
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Fatal)
                    // Framework chatter outside an API call (e.g. the background log-mode check) is noise.
                    .Filter.ByExcluding(e => e.Level < LogEventLevel.Warning
                                             && fromFramework(e)
                                             && !fromHostLifetime(e)
                                             && !e.Properties.ContainsKey(LogFields.CorrelationId))
                    // The file is one valid JSON array (paste the whole file into any JSON viewer); the
                    // console (from configuration) stays as readable per-entry objects.
                    .WriteTo.Sink(new JsonArrayFileSink("logs", new AppJsonFormatter()))
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext();
            });
    }

    private static Serilog.ILogger StartupLogger =>
        Log.ForContext(AppJsonFormatter.LayerProperty, "Server").ForContext(AppJsonFormatter.MethodProperty, StartupMethod);

    /// <summary>A start-up fact sheet (environment, database, version …) as one entry, Method = Startup.</summary>
    public static void LogStartup(string message, IReadOnlyDictionary<string, object?> context)
    {
        var logger = StartupLogger;
        foreach (var (name, value) in context.Reverse())       // each ForContext goes in front: reverse keeps the given order
        {
            logger = logger.ForContext(name, value, destructureObjects: true);   // keep arrays and nested objects as JSON
        }

        logger.Information(message);
    }

    /// <summary>The API could not start (or stopped on an unhandled error): written and flushed before the process ends.</summary>
    public static void LogStartupFailure(Exception exception)
    {
        StartupLogger.Fatal(exception, "API failed to start: {Reason}", exception.Message);
        Log.CloseAndFlush();
    }

    /// <summary>Normal shutdown: the last entries reach the file.</summary>
    public static void LogStopped()
    {
        StartupLogger.Information("API stopped");
        Log.CloseAndFlush();
    }
}
