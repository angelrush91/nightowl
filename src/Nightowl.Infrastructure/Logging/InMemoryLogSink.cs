using Serilog.Core;
using Serilog.Events;

namespace Nightowl.Infrastructure.Logging;

public class InMemoryLogSink : ILogEventSink
{
    private readonly int _maxCapacity;
    private readonly List<LogEntryItem> _entries;
    private readonly object _lock = new();

    public event Action<LogEntryItem>? OnLogAdded;

    public InMemoryLogSink(int maxCapacity = 500)
    {
        _maxCapacity = maxCapacity;
        _entries = new List<LogEntryItem>(maxCapacity);
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null) return;

        string? sourceContext = null;
        if (logEvent.Properties.TryGetValue("SourceContext", out var scValue))
        {
            sourceContext = scValue.ToString().Trim('\"');
            var lastDot = sourceContext.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < sourceContext.Length - 1)
            {
                sourceContext = sourceContext[(lastDot + 1)..];
            }
        }

        var item = new LogEntryItem(
            Timestamp: logEvent.Timestamp,
            Level: logEvent.Level,
            Message: logEvent.RenderMessage(),
            Exception: logEvent.Exception?.ToString(),
            SourceContext: sourceContext
        );

        lock (_lock)
        {
            if (_entries.Count >= _maxCapacity)
            {
                _entries.RemoveAt(0);
            }
            _entries.Add(item);
        }

        try
        {
            OnLogAdded?.Invoke(item);
        }
        catch
        {
            // Ignore subscriber callback errors
        }
    }

    public IReadOnlyList<LogEntryItem> GetEntries()
    {
        lock (_lock)
        {
            return _entries.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }
}
