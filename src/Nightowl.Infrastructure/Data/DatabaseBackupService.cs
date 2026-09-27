using Microsoft.Extensions.Logging;
using Nightowl.Application.Interfaces;

namespace Nightowl.Infrastructure.Data;

/// <summary>
/// Handles exporting and restoring the SQLite database file.
/// </summary>
public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly string _activeDbPath;
    private readonly ILogger<DatabaseBackupService>? _logger;

    public DatabaseBackupService(string activeDbPath, ILogger<DatabaseBackupService>? logger = null)
    {
        _activeDbPath = !string.IsNullOrWhiteSpace(activeDbPath)
            ? activeDbPath
            : throw new ArgumentException("Active database path must not be null or empty.", nameof(activeDbPath));
        _logger = logger;
    }

    public string GetDatabasePath() => _activeDbPath;

    public Task<string> CreateBackupAsync(string targetDirectory, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_activeDbPath))
        {
            throw new FileNotFoundException($"Cannot create backup: active database file not found at '{_activeDbPath}'.");
        }

        Directory.CreateDirectory(targetDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var fileName = $"nightowl-backup-{timestamp}.db";
        var backupPath = Path.Combine(targetDirectory, fileName);

        _logger?.LogInformation("Creating database backup from {Source} to {Target}", _activeDbPath, backupPath);

        // Copy active database file to target backup file
        File.Copy(_activeDbPath, backupPath, overwrite: true);

        return Task.FromResult(backupPath);
    }

    public Task RestoreBackupAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"Cannot restore database: backup file not found at '{sourceFilePath}'.");
        }

        var fileInfo = new FileInfo(sourceFilePath);
        if (fileInfo.Length == 0)
        {
            throw new InvalidOperationException("The specified backup file is empty and cannot be restored.");
        }

        var targetDir = Path.GetDirectoryName(_activeDbPath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Clean up any lingering SQLite WAL / SHM files for the active DB so it doesn't replay stale state
        var walFile = _activeDbPath + "-wal";
        var shmFile = _activeDbPath + "-shm";

        if (File.Exists(walFile))
        {
            try { File.Delete(walFile); } catch { /* best effort */ }
        }

        if (File.Exists(shmFile))
        {
            try { File.Delete(shmFile); } catch { /* best effort */ }
        }

        _logger?.LogInformation("Restoring database from {Source} to {Target}", sourceFilePath, _activeDbPath);

        File.Copy(sourceFilePath, _activeDbPath, overwrite: true);

        return Task.CompletedTask;
    }
}
