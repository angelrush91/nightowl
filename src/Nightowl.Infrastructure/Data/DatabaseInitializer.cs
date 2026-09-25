using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nightowl.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly NightowlDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(NightowlDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Ensuring Nightowl SQLite database exists and schema is up to date...");
            await _context.Database.EnsureCreatedAsync(cancellationToken);
            _logger.LogInformation("Nightowl database is ready.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Nightowl SQLite database.");
            throw;
        }
    }
}
