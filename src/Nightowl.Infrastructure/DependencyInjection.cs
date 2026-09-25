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

        // HTTP Clients for open-source ISBN APIs
        services.AddHttpClient<OpenLibraryIsbnService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nightowl/1.0 (Book cataloging app)");
        });

        services.AddHttpClient<GoogleBooksFallbackIsbnService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nightowl/1.0 (Book cataloging app)");
        });

        // ISBN Lookup Composite Service
        services.AddScoped<IIsbnLookupService, CompositeIsbnLookupService>();

        // Application Services
        services.AddScoped<IBookService, BookService>();

        return services;
    }
}
