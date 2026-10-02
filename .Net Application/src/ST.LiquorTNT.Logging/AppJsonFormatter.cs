using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;

namespace ST.LiquorTNT.Logging;

/// <summary>
/// Writes every log entry with the same keys, indented so a person can read it:
/// <code>
/// {
///   "Timestamp": "2026-09-28 17:31:38.070",
///   "Level": "Warning",
///   "CorrelationId": "5de6eb2cc8964c02960f3846692f02e5",
///   "Method": "AuthService.LoginAsync",
///   "Message": "Refused: INVALID_CREDENTIALS User name or password is incorrect. (87 ms)",
///   "Context": { "input": { "request": { "userName": "ravi", "password": "***" } } }
/// }
/// </code>
/// <c>Exception</c> is added only when there is one. Time is plain IST. <c>Context</c> holds every value
/// that is not already in the message — bodies, inputs and outputs as JSON objects, SQL as a list of lines —
/// and is <c>null</c> when there is nothing. Framework plumbing (RequestId, ConnectionId …) is left out.
/// </summary>
public class AppJsonFormatter : ITextFormatter
{
    public const string MethodProperty = "Method";
    public const string LayerProperty = "Layer";
    public const string SqlProperty = "Sql";

    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private const int MaxStackLines = 20;

    private readonly JsonWriterOptions _writerOptions;

    /// <summary>Indented, readable output (the default).</summary>
    public AppJsonFormatter() : this(indented: true)
    {
    }

    /// <summary>Indented for people, or one line per entry (NDJSON) for tools.</summary>
    protected AppJsonFormatter(bool indented) =>
        _writerOptions = new JsonWriterOptions { Indented = indented, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Properties that hold JSON text (already masked by <see cref="SafeJson"/>): written as JSON.</summary>
    private static readonly HashSet<string> JsonProperties = new(StringComparer.Ordinal)
    {
        "Request", "Response", "Input", "Output", "Steps", "FailedAt",
    };

    /// <summary>Framework plumbing nobody reads while troubleshooting a call.</summary>
    private static readonly HashSet<string> Hidden = new(StringComparer.Ordinal)
    {
        "RequestId", "RequestPath", "ConnectionId", "SourceContext", "EventId", "ActionId", "ActionName",
        "TraceId", "SpanId", "ParentId", MethodProperty, LayerProperty, LogFields.CorrelationId,
    };

    private static readonly TimeZoneInfo India = FindIndia();

    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer, _writerOptions))
        {
            json.WriteStartObject();
            json.WriteString("Timestamp", TimeZoneInfo.ConvertTime(logEvent.Timestamp, India).ToString(TimeFormat, CultureInfo.InvariantCulture));
            json.WriteString("Level", LevelName(logEvent.Level));
            json.WriteString("CorrelationId", Text(logEvent, LogFields.CorrelationId));
            json.WriteString("Layer", Text(logEvent, LayerProperty));
            json.WriteString("Method", MethodOf(logEvent));

            var inMessage = new HashSet<string>(StringComparer.Ordinal);
            json.WriteString("Message", Render(logEvent, inMessage));
            WriteContext(json, logEvent, inMessage);

            if (logEvent.Exception is not null)
            {
                WriteException(json, logEvent.Exception);
            }

            json.WriteEndObject();
        }

        output.Write(Encoding.UTF8.GetString(buffer.ToArray()));
        output.WriteLine();
    }

    /// <summary>Who wrote the entry: the Method property when set, otherwise the logging class.</summary>
    private static string MethodOf(LogEvent logEvent)
    {
        if (Text(logEvent, MethodProperty) is { Length: > 0 } method)
        {
            return method;
        }

        var source = Source(logEvent);
        return source switch
        {
            null => "App",
            "Microsoft.Hosting.Lifetime" => "Startup",
            _ => source[(source.LastIndexOf('.') + 1)..],
        };
    }

    private static void WriteContext(Utf8JsonWriter json, LogEvent logEvent, HashSet<string> inMessage)
    {
        var fields = logEvent.Properties
            .Where(p => !Hidden.Contains(p.Key) && !inMessage.Contains(p.Key) && !IsEmpty(p.Value))
            .ToList();

        if (fields.Count == 0)
        {
            json.WriteNull("Context");
            return;
        }

        json.WriteStartObject("Context");
        foreach (var (name, value) in fields)
        {
            json.WritePropertyName(CamelCase(name));

            if (name == SqlProperty && value is ScalarValue { Value: string sql })
            {
                WriteLines(json, sql);       // the runnable query, one array item per line
            }
            else
            {
                WriteValue(json, value, asJson: JsonProperties.Contains(name));
            }
        }
        json.WriteEndObject();
    }

    private static void WriteLines(Utf8JsonWriter json, string text)
    {
        json.WriteStartArray();
        foreach (var line in text.Split('\n'))
        {
            if (line.TrimEnd('\r') is { Length: > 0 } trimmed)
            {
                json.WriteStringValue(trimmed);
            }
        }
        json.WriteEndArray();
    }

    private static void WriteException(Utf8JsonWriter json, Exception exception)
    {
        var innermost = exception;
        while (innermost.InnerException is not null)
        {
            innermost = innermost.InnerException;
        }

        json.WriteStartObject("Exception");
        json.WriteString("ExceptionType", exception.GetType().FullName);
        json.WriteString("ExceptionMessage", exception.Message);
        json.WriteString("InnerExceptionType", ReferenceEquals(innermost, exception) ? null : innermost.GetType().FullName);
        json.WriteString("InnerException", ReferenceEquals(innermost, exception) ? null : innermost.Message);

        // Line 0 repeats type + message. Keep only our own frames (where a fix would go); the driver and
        // framework plumbing in between is noise. If a fault is entirely in the framework, keep the top lines.
        var all = exception.ToString().Split('\n').Skip(1).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var ours = all.Where(IsAppFrame).ToList();
        var lines = ours.Count > 0 ? ours : all.Take(MaxStackLines).ToList();

        json.WriteStartArray("StackTrace");
        foreach (var line in lines.Take(MaxStackLines))
        {
            json.WriteStringValue(line);
        }
        if (lines.Count > MaxStackLines)
        {
            json.WriteStringValue($"… {lines.Count - MaxStackLines} more lines");
        }
        json.WriteEndArray();

        json.WriteEndObject();
    }

    /// <summary>A stack frame in our own code — where a fix would go — but not the logging proxy, which is on every trace.</summary>
    private static bool IsAppFrame(string line) =>
        line.Contains("ST.LiquorTNT.", StringComparison.Ordinal) &&
        !line.Contains("ST.LiquorTNT.Api.Logging.MethodLoggingProxy", StringComparison.Ordinal);

    /// <summary>The message with its values filled in (text unquoted); remembers which properties it used.</summary>
    private static string Render(LogEvent logEvent, HashSet<string> used)
    {
        var text = new StringBuilder();

        foreach (var token in logEvent.MessageTemplate.Tokens)
        {
            if (token is TextToken literal)
            {
                text.Append(literal.Text);
            }
            else if (token is PropertyToken property && logEvent.Properties.TryGetValue(property.PropertyName, out var value))
            {
                used.Add(property.PropertyName);
                text.Append(value is ScalarValue { Value: string s } ? s : value.ToString(property.Format, CultureInfo.InvariantCulture));
            }
            else
            {
                text.Append(token);
            }
        }

        return text.ToString();
    }

    private static void WriteValue(Utf8JsonWriter json, LogEventPropertyValue value, bool asJson)
    {
        switch (value)
        {
            case ScalarValue { Value: string s } when asJson && TryWriteJson(json, s):
                break;
            case ScalarValue scalar:
                WriteScalar(json, scalar.Value);
                break;
            case SequenceValue sequence:
                json.WriteStartArray();
                foreach (var item in sequence.Elements)
                {
                    WriteValue(json, item, asJson: false);
                }
                json.WriteEndArray();
                break;
            case StructureValue structure:
                json.WriteStartObject();
                foreach (var property in structure.Properties)
                {
                    json.WritePropertyName(CamelCase(property.Name));
                    WriteValue(json, property.Value, asJson: false);
                }
                json.WriteEndObject();
                break;
            case DictionaryValue dictionary:
                json.WriteStartObject();
                foreach (var (key, item) in dictionary.Elements)
                {
                    json.WritePropertyName(key.Value?.ToString() ?? "null");
                    WriteValue(json, item, asJson: false);
                }
                json.WriteEndObject();
                break;
            default:
                json.WriteStringValue(value.ToString());
                break;
        }
    }

    private static bool TryWriteJson(Utf8JsonWriter json, string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            document.RootElement.WriteTo(json);
            return true;
        }
        catch (JsonException)
        {
            return false;      // e.g. "<body not logged: …>" — written as plain text instead
        }
    }

    private static void WriteScalar(Utf8JsonWriter json, object? value)
    {
        switch (value)
        {
            case null: json.WriteNullValue(); break;
            case bool b: json.WriteBooleanValue(b); break;
            case int or long or short or byte or uint or ulong or ushort or sbyte:
                json.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture)); break;
            case double or float or decimal:
                json.WriteNumberValue(Convert.ToDecimal(value, CultureInfo.InvariantCulture)); break;
            case DateTime dt: json.WriteStringValue(dt.ToString(TimeFormat, CultureInfo.InvariantCulture)); break;
            case DateTimeOffset dto: json.WriteStringValue(TimeZoneInfo.ConvertTime(dto, India).ToString(TimeFormat, CultureInfo.InvariantCulture)); break;
            default: json.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture)); break;
        }
    }

    private static string LevelName(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "Trace",
        LogEventLevel.Debug => "Debug",
        LogEventLevel.Information => "Info",
        LogEventLevel.Warning => "Warning",
        LogEventLevel.Error => "Error",
        _ => "Fatal",
    };

    private static string? Source(LogEvent logEvent) => Text(logEvent, "SourceContext");

    private static string? Text(LogEvent logEvent, string property) =>
        logEvent.Properties.TryGetValue(property, out var value)
            ? value is ScalarValue scalar ? Convert.ToString(scalar.Value, CultureInfo.InvariantCulture) : value.ToString()
            : null;

    private static bool IsEmpty(LogEventPropertyValue value) =>
        value is ScalarValue { Value: null } or ScalarValue { Value: "" };

    private static string CamelCase(string name) =>
        name.Length == 0 || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static TimeZoneInfo FindIndia()
    {
        foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromMinutes(330), "India Standard Time", "IST");
    }
}
