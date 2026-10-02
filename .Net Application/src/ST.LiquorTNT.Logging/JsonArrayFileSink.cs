using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace ST.LiquorTNT.Logging;

/// <summary>
/// Writes the log file as one valid JSON array, so the whole file can be pasted into any JSON viewer
/// (jsoncrack, etc.) without a "multiple roots" error. The file always ends with <c>]</c>: each new entry
/// is inserted just before it, so even if the process is killed the file stays valid JSON.
/// One file per day (<c>stliquortnt-YYYYMMDD.json</c>); the newest 30 are kept.
/// </summary>
public sealed class JsonArrayFileSink : ILogEventSink, IDisposable
{
    private static readonly byte[] Empty = Encoding.UTF8.GetBytes("[\n]");   // a valid, empty array
    private static readonly byte[] OpenBracket = Encoding.UTF8.GetBytes("[\n");
    private static readonly byte[] CloseBracket = Encoding.UTF8.GetBytes("\n]");
    private static readonly byte[] Separator = Encoding.UTF8.GetBytes(",\n");

    private static readonly TimeZoneInfo India = FindIndia();

    private readonly string _directory;
    private readonly string _prefix;
    private readonly ITextFormatter _formatter;
    private readonly int _retainedFiles;
    private readonly object _gate = new();

    private string? _currentPath;
    private FileStream? _stream;
    private bool _hasEntries;

    public JsonArrayFileSink(string directory, ITextFormatter formatter, string prefix = "stliquortnt-", int retainedFiles = 30)
    {
        _directory = directory;
        _prefix = prefix;
        _formatter = formatter;
        _retainedFiles = retainedFiles;
    }

    public void Emit(LogEvent logEvent)
    {
        var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var entry = Encoding.UTF8.GetBytes(writer.ToString().Trim('\r', '\n', ' '));   // the object, no trailing newline

        lock (_gate)
        {
            OpenForDay(logEvent.Timestamp);
            if (_stream is null)
            {
                return;
            }

            if (!_hasEntries)
            {
                _stream.Seek(OpenBracket.Length, SeekOrigin.Begin);     // just after "[\n", over the empty "]"
                _stream.Write(entry);
                _stream.Write(CloseBracket);
                _hasEntries = true;
            }
            else
            {
                _stream.Seek(-CloseBracket.Length, SeekOrigin.End);     // over the trailing "\n]"
                _stream.Write(Separator);
                _stream.Write(entry);
                _stream.Write(CloseBracket);
            }

            _stream.SetLength(_stream.Position);
            _stream.Flush(flushToDisk: true);
        }
    }

    private void OpenForDay(DateTimeOffset timestamp)
    {
        var day = TimeZoneInfo.ConvertTime(timestamp, India).ToString("yyyyMMdd");
        var path = Path.Combine(_directory, $"{_prefix}{day}.json");

        if (path == _currentPath && _stream is not null)
        {
            return;
        }

        _stream?.Dispose();
        Directory.CreateDirectory(_directory);
        _currentPath = path;

        var reopen = File.Exists(path) && new FileInfo(path).Length >= Empty.Length && StartsWithArray(path);
        _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

        if (reopen)
        {
            _hasEntries = _stream.Length > Empty.Length;                // more than the empty "[\n]" → already has entries
        }
        else
        {
            _stream.SetLength(0);                                       // new file, or an old non-array file: start clean
            _stream.Write(Empty);
            _stream.Flush(flushToDisk: true);
            _hasEntries = false;
        }

        CleanupOldFiles();
    }

    private static bool StartsWithArray(string path)
    {
        try
        {
            using var probe = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return probe.ReadByte() == '[';
        }
        catch (IOException)
        {
            return false;
        }
    }

    private void CleanupOldFiles()
    {
        try
        {
            foreach (var old in Directory.GetFiles(_directory, $"{_prefix}*.json")
                         .OrderByDescending(f => f)
                         .Skip(_retainedFiles))
            {
                File.Delete(old);
            }
        }
        catch (IOException)
        {
            // a file in use or already gone — retention is best-effort, never fail a log write.
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _stream?.Dispose();
            _stream = null;
        }
    }

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
