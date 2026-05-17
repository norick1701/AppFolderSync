using FolderSyncModule.Library;
using Xunit;

namespace HelloWorldApp.Tests;
public class FileSystemOperationsTests : IDisposable
{
    private readonly string _testDir;
    private readonly FileSystemOperations _fileOps;

    public FileSystemOperationsTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"FolderSyncTest_{Guid.NewGuid()}");
        _fileOps = new FileSystemOperations();
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void CopyFile_ShouldCopyFileSuccessfully()
    {
        // Arrange
        string sourceFile = Path.Combine(_testDir, "source.txt");
        string targetFile = Path.Combine(_testDir, "target", "target.txt");
        File.WriteAllText(sourceFile, "test content");

        // Act
        _fileOps.CopyFile(sourceFile, targetFile);

        // Assert
        Assert.True(File.Exists(targetFile));
        Assert.Equal("test content", File.ReadAllText(targetFile));
    }

    [Fact]
    public void CreateDirectory_ShouldCreateDirectorySuccessfully()
    {
        // Arrange
        string dirPath = Path.Combine(_testDir, "newdir");

        // Act
        _fileOps.CreateDirectory(dirPath);

        // Assert
        Assert.True(Directory.Exists(dirPath));
    }

    [Fact]
    public void DeleteFile_ShouldDeleteFileSuccessfully()
    {
        // Arrange
        string filePath = Path.Combine(_testDir, "delete.txt");
        File.WriteAllText(filePath, "content");

        // Act
        _fileOps.DeleteFile(filePath);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void FileExists_ShouldReturnTrueWhenFileExists()
    {
        // Arrange
        string filePath = Path.Combine(_testDir, "exists.txt");
        File.WriteAllText(filePath, "content");

        // Act
        bool exists = _fileOps.FileExists(filePath);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void FileExists_ShouldReturnFalseWhenFileDoesNotExist()
    {
        // Arrange
        string filePath = Path.Combine(_testDir, "notexists.txt");

        // Act
        bool exists = _fileOps.FileExists(filePath);

        // Assert
        Assert.False(exists);
    }
}
