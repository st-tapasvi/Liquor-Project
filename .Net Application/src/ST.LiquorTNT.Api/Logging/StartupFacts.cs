using System.Data.Common;
using System.Reflection;

namespace ST.LiquorTNT.Api.Logging;

/// <summary>
/// What the first log entry says about this installation, so a start-up problem on IIS or a customer
/// server can be read from the log alone: which build, which environment, which database. The connection
/// string is never logged — only its server and database name.
/// </summary>
public static class StartupFacts
{
    public static IReadOnlyDictionary<string, object?> From(WebApplication app, IEnumerable<string> urls)
    {
        var (server, database) = Database(app.Configuration.GetConnectionString("Default"));

        return new Dictionary<string, object?>
        {
            ["Version"] = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            ["Environment"] = app.Environment.EnvironmentName,
            ["Machine"] = System.Environment.MachineName,
            ["Urls"] = urls.ToArray(),
            ["Database"] = new Dictionary<string, object?>
            {
                ["provider"] = app.Configuration["Database:Provider"],
                ["server"] = server,
                ["name"] = database,
            },
            ["ContentRoot"] = app.Environment.ContentRootPath,
            ["DotNet"] = System.Environment.Version.ToString(),
            ["ProcessId"] = System.Environment.ProcessId,
        };
    }

    private static (string? Server, string? Database) Database(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return ("<ConnectionStrings:Default is missing>", null);
        }

        try
        {
            var parts = new DbConnectionStringBuilder { ConnectionString = connectionString };
            return (Value(parts, "Server", "Data Source", "Host"), Value(parts, "Database", "Initial Catalog"));
        }
        catch (ArgumentException)
        {
            return ("<ConnectionStrings:Default cannot be read>", null);
        }
    }

    private static string? Value(DbConnectionStringBuilder parts, params string[] keys) =>
        keys.Select(k => parts.TryGetValue(k, out var v) ? v?.ToString() : null).FirstOrDefault(v => v is not null);
}
