using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Nightowl.Infrastructure;
using Nightowl.Infrastructure.Data;
using Nightowl.Infrastructure.Logging;
using Serilog;
using Serilog.Events;

namespace Nightowl.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Initialize SQLite native provider
        SQLitePCL.Batteries_V2.Init();

        // Setup Serilog with file sink and in-memory ring-buffer sink
        var inMemorySink = new InMemoryLogSink(maxCapacity: 500);
        var logDir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        try
        {
            Directory.CreateDirectory(logDir);
        }
        catch {}

        var logFilePath = Path.Combine(logDir, "nightowl-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Sink(inMemorySink)
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        Log.Information("Nightowl initializing on platform: {DevicePlatform}", DeviceInfo.Platform);

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: true);

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        // Register Dev Logging Services
        builder.Services.AddSingleton<InMemoryLogSink>(inMemorySink);
        builder.Services.AddSingleton<IDevLogService>(sp => new DevLogService(
            inMemorySink, 
            logDir,
            getDevMode: () => Preferences.Default.Get("nightowl_dev_mode_enabled", false),
            setDevMode: val => Preferences.Default.Set("nightowl_dev_mode_enabled", val)
        ));

#if ANDROID
        Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("AllowCamera", (handler, view) =>
        {
            if (handler.PlatformView is global::Android.Webkit.WebView webView)
            {
                webView.SetWebChromeClient(new Platforms.Android.PermissionWebChromeClient());
            }
        });
#endif

        // Safe SQLite database path in AppData directory
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "nightowl.db");
        builder.Services.AddNightowlInfrastructure(dbPath);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        var app = builder.Build();

        // Asynchronously initialize database in background without blocking main UI thread
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                await initializer.InitializeAsync();
                Log.Information("Database initialized successfully at {DbPath}", dbPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization error: {ErrorMessage}", ex.Message);
            }
        });

        return app;
    }
}
