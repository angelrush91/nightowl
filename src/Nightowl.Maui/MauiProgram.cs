using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Nightowl.Infrastructure;
using Nightowl.Infrastructure.Data;

#if ANDROID
using Nightowl.Maui.Platforms.Android;
#endif

namespace Nightowl.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        // SQLite database path in AppData directory
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "nightowl.db");
        builder.Services.AddNightowlInfrastructure(dbPath);

#if ANDROID
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("AllowCamera", (handler, view) =>
        {
            handler.PlatformView.SetWebChromeClient(new PermissionWebChromeClient());
        });
#endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Ensure database is initialized
        using (var scope = app.Services.CreateScope())
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
            initializer.InitializeAsync().GetAwaiter().GetResult();
        }

        return app;
    }
}
