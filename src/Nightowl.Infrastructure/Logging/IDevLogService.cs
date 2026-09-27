using Serilog.Events;

namespace Nightowl.Infrastructure.Logging;

public interface IDevLogService
{
    bool IsDevModeEnabled { get; set; }
    IReadOnlyList<LogEntryItem> GetLogs(string? search = null, LogEventLevel? minLevel = null);
    void ClearLogs();
    string ExportLogsAsText();
    string GetLogDirectory();
    event Action<LogEntryItem>? OnLogAdded;
}
