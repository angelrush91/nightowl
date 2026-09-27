using Nightowl.Infrastructure.Data;
using Xunit;

namespace Nightowl.Tests.Infrastructure;

public class DatabaseBackupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _activeDbPath;

    public DatabaseBackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NightowlBackupTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _activeDbPath = Path.Combine(_tempDir, "nightowl.db");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void GetDatabasePath_ReturnsConfiguredPath()
    {
        var service = new DatabaseBackupService(_activeDbPath);
        Assert.Equal(_activeDbPath, service.GetDatabasePath());
    }

    [Fact]
    public async Task CreateBackupAsync_WhenDbDoesNotExist_ThrowsFileNotFoundException()
    {
        var service = new DatabaseBackupService(_activeDbPath);
        var targetDir = Path.Combine(_tempDir, "backups");

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.CreateBackupAsync(targetDir));
    }

    [Fact]
    public async Task CreateBackupAsync_WhenDbExists_CreatesBackupCopy()
    {
        await File.WriteAllTextAsync(_activeDbPath, "SQLITE-TEST-DB-CONTENT");
        var service = new DatabaseBackupService(_activeDbPath);
        var targetDir = Path.Combine(_tempDir, "backups");

        var backupPath = await service.CreateBackupAsync(targetDir);

        Assert.True(File.Exists(backupPath));
        Assert.StartsWith(targetDir, backupPath);
        Assert.EndsWith(".db", backupPath);

        var content = await File.ReadAllTextAsync(backupPath);
        Assert.Equal("SQLITE-TEST-DB-CONTENT", content);
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenSourceDoesNotExist_ThrowsFileNotFoundException()
    {
        var service = new DatabaseBackupService(_activeDbPath);
        var missingSource = Path.Combine(_tempDir, "does-not-exist.db");

        await Assert.ThrowsAsync<FileNotFoundException>(() => service.RestoreBackupAsync(missingSource));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenSourceIsEmpty_ThrowsInvalidOperationException()
    {
        var emptySource = Path.Combine(_tempDir, "empty.db");
        await File.WriteAllBytesAsync(emptySource, Array.Empty<byte>());
        var service = new DatabaseBackupService(_activeDbPath);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreBackupAsync(emptySource));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenValidBackup_OverwritesTargetDatabase()
    {
        await File.WriteAllTextAsync(_activeDbPath, "OLD-ACTIVE-DATABASE");
        var sourceBackup = Path.Combine(_tempDir, "valid-backup.db");
        await File.WriteAllTextAsync(sourceBackup, "NEW-RESTORED-DATABASE");

        var service = new DatabaseBackupService(_activeDbPath);
        await service.RestoreBackupAsync(sourceBackup);

        Assert.True(File.Exists(_activeDbPath));
        var content = await File.ReadAllTextAsync(_activeDbPath);
        Assert.Equal("NEW-RESTORED-DATABASE", content);
    }
}
