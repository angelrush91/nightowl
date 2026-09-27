using System.Text;
using Serilog.Events;

namespace Nightowl.Infrastructure.Logging;

public class DevLogService : IDevLogService
{
    private readonly InMemoryLogSink _logSink;
    private readonly string _logDirectory;
    private readonly Func<bool>? _getDevMode;
    private readonly Action<bool>? _setDevMode;
    private bool _inMemoryDevMode;

    public event Action<LogEntryItem>? OnLogAdded;

    public DevLogService(
        InMemoryLogSink logSink,
        string logDirectory,
        Func<bool>? getDevMode = null,
        Action<bool>? setDevMode = null)
    {
        _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
        _logDirectory = logDirectory ?? string.Empty;
        _getDevMode = getDevMode;
        _setDevMode = setDevMode;
        _logSink.OnLogAdded += item => OnLogAdded?.Invoke(item);
    }

    public bool IsDevModeEnabled
    {
        get => _getDevMode != null ? _getDevMode() : _inMemoryDevMode;
        set
        {
            if (_setDevMode != null)
            {
                _setDevMode(value);
            }
            else
            {
                _inMemoryDevMode = value;
            }
        }
    }

    public IReadOnlyList<LogEntryItem> GetLogs(string? search = null, LogEventLevel? minLevel = null)
    {
        var all = _logSink.GetEntries();
        IEnumerable<LogEntryItem> query = all;

        if (minLevel.HasValue)
        {
            query = query.Where(e => e.Level >= minLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.Message.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.SourceContext != null && e.SourceContext.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.Exception != null && e.Exception.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

        return query.OrderByDescending(e => e.Timestamp).ToList();
    }

    public void ClearLogs()
    {
        _logSink.Clear();
    }

    public string ExportLogsAsText()
    {
        var logs = _logSink.GetEntries();
        var sb = new StringBuilder();
        sb.AppendLine($"--- Nightowl Application Logs ({DateTime.UtcNow:u}) ---");
        sb.AppendLine($"Total Captured Entries: {logs.Count}");
        sb.AppendLine();

        foreach (var entry in logs)
        {
            var levelTag = entry.Level switch
            {
                LogEventLevel.Verbose => "VERB ",
                LogEventLevel.Debug => "DEBUG",
                LogEventLevel.Information => "INFO ",
                LogEventLevel.Warning => "WARN ",
                LogEventLevel.Error => "ERROR",
                LogEventLevel.Fatal => "FATAL",
                _ => entry.Level.ToString().ToUpperInvariant()
            };
            var time = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            sb.AppendLine($"[{time}] [{levelTag}] [{(entry.SourceContext ?? "App")}] {entry.Message}");
            if (!string.IsNullOrWhiteSpace(entry.Exception))
            {
                sb.AppendLine("  Exception: " + entry.Exception);
            }
        }

        return sb.ToString();
    }

    public string GetLogDirectory() => _logDirectory;
}
