using FolderSyncModule.Library;
using Xunit;

namespace HelloWorldApp.Tests;
public class DiffDetectorTests : IDisposable
{
    private readonly string _testDir;
    private readonly DiffDetector _detector;

    public DiffDetectorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"DiffDetectorTest_{Guid.NewGuid()}");
        _detector = new DiffDetector();
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void NeedsSync_ShouldReturnTrueWhenTargetDoesNotExist()
    {
        // Arrange
        string sourceFile = Path.Combine(_testDir, "source.txt");
        string targetFile = Path.Combine(_testDir, "target.txt");
        File.WriteAllText(sourceFile, "content");

        // Act
        bool result = _detector.NeedsSync(sourceFile, targetFile);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NeedsSync_ShouldReturnTrueWhenSourceIsNewer()
    {
        // Arrange
        string sourceFile = Path.Combine(_testDir, "source.txt");
        string targetFile = Path.Combine(_testDir, "target.txt");
        File.WriteAllText(sourceFile, "source");
        File.WriteAllText(targetFile, "target");

        // ソースをターゲットより新しくする
        System.Threading.Thread.Sleep(100);
        File.WriteAllText(sourceFile, "updated source");

        // Act
        bool result = _detector.NeedsSync(sourceFile, targetFile);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NeedsSync_ShouldReturnFalseWhenTargetIsNewer()
    {
        // Arrange
        string sourceFile = Path.Combine(_testDir, "source.txt");
        string targetFile = Path.Combine(_testDir, "target.txt");
        File.WriteAllText(sourceFile, "source");

        System.Threading.Thread.Sleep(100);
        File.WriteAllText(targetFile, "target");

        // Act
        bool result = _detector.NeedsSync(sourceFile, targetFile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetOrphanedFiles_ShouldReturnFilesNotInSource()
    {
        // Arrange
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(targetDir, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(targetDir, "orphan.txt"), "orphan");

        // Act
        var orphaned = _detector.GetOrphanedFiles(sourceDir, targetDir).ToList();

        // Assert
        Assert.Single(orphaned);
        Assert.Contains("orphan.txt", orphaned.First());
    }

    [Fact]
    public void GetOrphanedDirectories_ShouldReturnDirectoriesNotInSource()
    {
        // Arrange
        string sourceDir = Path.Combine(_testDir, "source");
        string targetDir = Path.Combine(_testDir, "target");
        Directory.CreateDirectory(Path.Combine(sourceDir, "dir1"));
        Directory.CreateDirectory(Path.Combine(targetDir, "dir1"));
        Directory.CreateDirectory(Path.Combine(targetDir, "orphandir"));

        // Act
        var orphaned = _detector.GetOrphanedDirectories(sourceDir, targetDir).ToList();

        // Assert
        Assert.Single(orphaned);
        Assert.Contains("orphandir", orphaned.First());
    }
}
