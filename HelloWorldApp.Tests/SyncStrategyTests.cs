using FolderSyncModule.Library;
using Xunit;

namespace HelloWorldApp.Tests;

public class SyncStrategyTests : IDisposable
{
    private readonly string _testDir;

    public SyncStrategyTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"SyncStrategyTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void FileOnlyStrategy_ShouldCopyAllFiles()
    {
        // Arrange
        var strategy = new FileOnlySyncStrategy();
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "content2");

        // Act
        strategy.SyncFiles(sourceDir, targetDir);

        // Assert
        Assert.True(File.Exists(Path.Combine(targetDir, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(targetDir, "file2.txt")));
    }

    [Fact]
    public void FileOnlyStrategy_ShouldNotDeleteOrphanedFiles()
    {
        // Arrange
        var strategy = new FileOnlySyncStrategy();
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(targetDir, "orphan.txt"), "orphan");

        // Act
        strategy.DeleteOrphanedFiles(sourceDir, targetDir);

        // Assert
        Assert.True(File.Exists(Path.Combine(targetDir, "orphan.txt")));
    }

    [Fact]
    public void WithDeletionStrategy_ShouldDeleteOrphanedFiles()
    {
        // Arrange
        var strategy = new WithDeletionSyncStrategy();
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(targetDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(targetDir, "orphan.txt"), "orphan");

        // Act
        strategy.DeleteOrphanedFiles(sourceDir, targetDir);

        // Assert
        Assert.False(File.Exists(Path.Combine(targetDir, "orphan.txt")));
        Assert.True(File.Exists(Path.Combine(targetDir, "file1.txt")));
    }

    [Fact]
    public void DiffOnlyStrategy_ShouldSkipUnchangedFiles()
    {
        // Arrange
        var strategy = new DiffOnlySyncStrategy();
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        string sourceFile = Path.Combine(sourceDir, "file.txt");
        string targetFile = Path.Combine(targetDir, "file.txt");
        File.WriteAllText(sourceFile, "content");
        File.WriteAllText(targetFile, "content");

        // ターゲットの方が新しくする
        System.Threading.Thread.Sleep(100);
        File.SetLastWriteTime(targetFile, DateTime.Now);

        long targetSizeBefore = new FileInfo(targetFile).Length;

        // Act
        strategy.SyncFiles(sourceDir, targetDir);

        // Assert
        long targetSizeAfter = new FileInfo(targetFile).Length;
        Assert.Equal(targetSizeBefore, targetSizeAfter);
    }

    [Fact]
    public void DiffOnlyStrategy_ShouldCopyNewFiles()
    {
        // Arrange
        var strategy = new DiffOnlySyncStrategy();
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(sourceDir, "new.txt"), "new content");

        // Act
        strategy.SyncFiles(sourceDir, targetDir);

        // Assert
        Assert.True(File.Exists(Path.Combine(targetDir, "new.txt")));
    }
}

