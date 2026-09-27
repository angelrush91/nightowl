using Serilog.Events;

namespace Nightowl.Infrastructure.Logging;

public record LogEntryItem(
    DateTimeOffset Timestamp,
    LogEventLevel Level,
    string Message,
    string? Exception = null,
    string? SourceContext = null
);
