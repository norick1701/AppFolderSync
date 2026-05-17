using FolderSyncModule.Library;
using Xunit;

namespace HelloWorldApp.Tests;
public class SyncCoordinatorIntegrationTests : IDisposable
{
    private readonly string _testDir;
    private readonly SyncCoordinator _coordinator;

    public SyncCoordinatorIntegrationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"SyncCoordinatorTest_{Guid.NewGuid()}");
        _coordinator = new SyncCoordinator();
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void Sync_OneWayFileOnly_ShouldCopyAllFiles()
    {
        // Arrange
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);

        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "content2");

        // Act
        _coordinator.Sync(sourceDir, targetDir, SyncMode.OneWay, SyncType.OneTime, SyncScope.FileOnly);

        // Assert
        Assert.True(File.Exists(Path.Combine(targetDir, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(targetDir, "file2.txt")));
    }

    [Fact]
    public void Sync_OneWayWithDeletion_ShouldDeleteOrphanedFiles()
    {
        // Arrange
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "content");
        File.WriteAllText(Path.Combine(targetDir, "orphan.txt"), "orphan");

        // Act
        _coordinator.Sync(sourceDir, targetDir, SyncMode.OneWay, SyncType.OneTime, SyncScope.WithDeletion);

        // Assert
        Assert.True(File.Exists(Path.Combine(targetDir, "file.txt")));
        Assert.False(File.Exists(Path.Combine(targetDir, "orphan.txt")));
    }

    [Fact]
    public void Sync_DiffOnly_ShouldSkipUnchangedFiles()
    {
        // Arrange
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        string sourceFile = Path.Combine(sourceDir, "file.txt");
        string targetFile = Path.Combine(targetDir, "file.txt");

        File.WriteAllText(sourceFile, "content");
        System.Threading.Thread.Sleep(100);
        File.WriteAllText(targetFile, "content");
        System.Threading.Thread.Sleep(100);
        File.SetLastWriteTime(targetFile, DateTime.Now);

        var targetModBefore = File.GetLastWriteTime(targetFile);

        // Act
        _coordinator.Sync(sourceDir, targetDir, SyncMode.OneWay, SyncType.OneTime, SyncScope.DiffOnly);

        // Assert
        var targetModAfter = File.GetLastWriteTime(targetFile);
        Assert.Equal(targetModBefore, targetModAfter);
    }

    [Fact]
    public void Sync_WithSubdirectories_ShouldSyncRecursively()
    {
        // Arrange
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(Path.Combine(sourceDir, "subdir"));

        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "root");
        File.WriteAllText(Path.Combine(sourceDir, "subdir", "file2.txt"), "sub");

        // Act
        _coordinator.Sync(sourceDir, targetDir, SyncMode.OneWay, SyncType.OneTime, SyncScope.FileOnly);

        // Assert
        Assert.True(File.Exists(Path.Combine(targetDir, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(targetDir, "subdir", "file2.txt")));
    }

    [Fact]
    public void Sync_TwoWay_ShouldSyncBothDirections()
    {
        // Arrange
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(sourceDir, "source_file.txt"), "from source");
        File.WriteAllText(Path.Combine(targetDir, "target_file.txt"), "from target");

        // Act
        _coordinator.Sync(sourceDir, targetDir, SyncMode.TwoWay, SyncType.OneTime, SyncScope.FileOnly);

        // Assert
        Assert.True(File.Exists(Path.Combine(targetDir, "source_file.txt")));
        Assert.True(File.Exists(Path.Combine(sourceDir, "target_file.txt")));
    }
}

