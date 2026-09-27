namespace Nightowl.Application.Interfaces;

/// <summary>
/// Service responsible for backing up and restoring the SQLite database.
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Gets the path to the active SQLite database file.
    /// </summary>
    string GetDatabasePath();

    /// <summary>
    /// Creates a timestamped backup copy of the active SQLite database in the specified directory.
    /// </summary>
    /// <param name="targetDirectory">Directory where the backup copy will be placed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full path to the created backup file.</returns>
    Task<string> CreateBackupAsync(string targetDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the active SQLite database by replacing it with the given backup file.
    /// </summary>
    /// <param name="sourceFilePath">The source backup .db file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RestoreBackupAsync(string sourceFilePath, CancellationToken cancellationToken = default);
}
