using FluentAssertions;
using Nightowl.Infrastructure.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class InMemoryLogSinkTests
{
    private static LogEvent CreateTestEvent(LogEventLevel level, string message, string? sourceContext = null, Exception? exception = null)
    {
        var parser = new MessageTemplateParser();
        var template = parser.Parse(message);
        var properties = new List<LogEventProperty>();
        if (!string.IsNullOrEmpty(sourceContext))
        {
            properties.Add(new LogEventProperty("SourceContext", new ScalarValue(sourceContext)));
        }

        return new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            exception,
            template,
            properties
        );
    }

    [Fact]
    public void Emit_ShouldStoreLogEntryInRingBuffer()
    {
        var sink = new InMemoryLogSink(maxCapacity: 10);
        var logEvent = CreateTestEvent(LogEventLevel.Information, "Test log message", "TestContext");

        sink.Emit(logEvent);

        var entries = sink.GetEntries();
        entries.Should().HaveCount(1);
        entries[0].Message.Should().Be("Test log message");
        entries[0].Level.Should().Be(LogEventLevel.Information);
        entries[0].SourceContext.Should().Be("TestContext");
    }

    [Fact]
    public void Emit_ShouldDropOldestEntries_WhenCapacityExceeded()
    {
        var sink = new InMemoryLogSink(maxCapacity: 3);

        sink.Emit(CreateTestEvent(LogEventLevel.Information, "Message 1"));
        sink.Emit(CreateTestEvent(LogEventLevel.Information, "Message 2"));
        sink.Emit(CreateTestEvent(LogEventLevel.Information, "Message 3"));
        sink.Emit(CreateTestEvent(LogEventLevel.Information, "Message 4")); // Should drop Message 1

        var entries = sink.GetEntries();
        entries.Should().HaveCount(3);
        entries.Select(e => e.Message).Should().ContainInOrder("Message 2", "Message 3", "Message 4");
    }

    [Fact]
    public void Clear_ShouldEmptyBuffer()
    {
        var sink = new InMemoryLogSink(maxCapacity: 10);
        sink.Emit(CreateTestEvent(LogEventLevel.Warning, "Warning message"));

        sink.GetEntries().Should().HaveCount(1);
        sink.Clear();
        sink.GetEntries().Should().BeEmpty();
    }

    [Fact]
    public void DevLogService_GetLogs_ShouldFilterByLevel()
    {
        var sink = new InMemoryLogSink(maxCapacity: 10);
        sink.Emit(CreateTestEvent(LogEventLevel.Debug, "Debug detail"));
        sink.Emit(CreateTestEvent(LogEventLevel.Information, "Info notice"));
        sink.Emit(CreateTestEvent(LogEventLevel.Warning, "Warning notice"));
        sink.Emit(CreateTestEvent(LogEventLevel.Error, "Fatal crash"));

        var devService = new DevLogService(sink, "test/logs");

        var warningAndAbove = devService.GetLogs(minLevel: LogEventLevel.Warning);
        warningAndAbove.Should().HaveCount(2);
        warningAndAbove.Select(l => l.Level).Should().Contain(new[] { LogEventLevel.Warning, LogEventLevel.Error });
    }

    [Fact]
    public void DevLogService_GetLogs_ShouldFilterBySearchQuery()
    {
        var sink = new InMemoryLogSink(maxCapacity: 10);
        sink.Emit(CreateTestEvent(LogEventLevel.Information, "Database initialized successfully", "DbInitializer"));
        sink.Emit(CreateTestEvent(LogEventLevel.Information, "Resolved ISBN metadata for 9780132350884", "BookResolver"));
        sink.Emit(CreateTestEvent(LogEventLevel.Error, "Camera scanner failed to focus", "CameraScanner"));

        var devService = new DevLogService(sink, "test/logs");

        var searchResult = devService.GetLogs(search: "ISBN");
        searchResult.Should().HaveCount(1);
        searchResult[0].SourceContext.Should().Be("BookResolver");
    }

    [Fact]
    public void DevLogService_ExportLogsAsText_ShouldContainAllEntriesFormatted()
    {
        var sink = new InMemoryLogSink(maxCapacity: 10);
        sink.Emit(CreateTestEvent(LogEventLevel.Information, "App started", "Program"));
        sink.Emit(CreateTestEvent(LogEventLevel.Error, "Sample error", "Repo", new InvalidOperationException("Boom")));

        var devService = new DevLogService(sink, "test/logs");

        var export = devService.ExportLogsAsText();
        export.Should().Contain("Nightowl Application Logs");
        export.Should().Contain("[INFO ] [Program] App started");
        export.Should().Contain("[ERROR] [Repo] Sample error");
        export.Should().Contain("InvalidOperationException: Boom");
    }

    [Fact]
    public void DevLogService_DevModeToggle_ShouldPersistState()
    {
        bool storedVal = false;
        var sink = new InMemoryLogSink(maxCapacity: 10);
        var devService = new DevLogService(
            sink, 
            "test/logs",
            getDevMode: () => storedVal,
            setDevMode: val => storedVal = val
        );

        devService.IsDevModeEnabled.Should().BeFalse();
        devService.IsDevModeEnabled = true;
        devService.IsDevModeEnabled.Should().BeTrue();
        storedVal.Should().BeTrue();
    }
}
