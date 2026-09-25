using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Nightowl.Infrastructure;
using Nightowl.Infrastructure.Data;

namespace Nightowl.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Initialize SQLite native provider
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        // Safe SQLite database path in AppData directory
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "nightowl.db");
        builder.Services.AddNightowlInfrastructure(dbPath);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Initialization Error: {ex.Message}");
            }
        });

        return app;
    }
}
