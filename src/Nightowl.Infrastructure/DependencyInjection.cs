using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nightowl.Application.Interfaces;
using Nightowl.Application.Services;
using Nightowl.Domain.Repositories;
using Nightowl.Infrastructure.Data;
using Nightowl.Infrastructure.ExternalServices;

namespace Nightowl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNightowlInfrastructure(this IServiceCollection services, string sqliteDbPath)
    {
        // EF Core SQLite setup
        services.AddDbContext<NightowlDbContext>(options =>
        {
            options.UseSqlite($"Data Source={sqliteDbPath}");
        });

        // Repository
        services.AddScoped<IBookRepository, BookRepository>();

        // Initializer
        services.AddScoped<DatabaseInitializer>();

        // Database Backup Service
        services.AddSingleton<IDatabaseBackupService>(sp => 
            new DatabaseBackupService(sqliteDbPath, sp.GetService<Microsoft.Extensions.Logging.ILogger<DatabaseBackupService>>()));

        // Memory Cache for ISBN metadata resolution
        services.AddMemoryCache();

        // Deep ISBN Metadata Resolver
        services.AddHttpClient<IBookMetadataResolver, BookMetadataResolver>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nightowl/1.0 (Book cataloging app)");
        });

        // Application Services
        services.AddScoped<IBookService, BookService>();

        return services;
    }
}
