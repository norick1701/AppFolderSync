using FolderSyncModule.Library;
using FolderSyncModule.Library.Models;

namespace HelloWorldApp.Tests;

/// <summary>
/// エラーハンドリングのテスト。
/// Phase 1の品質改善として、エラー時の動作を検証します。
/// </summary>
public class ErrorHandlingTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly string _sourcePath;
    private readonly string _targetPath;

    public ErrorHandlingTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), "FolderSyncTests_ErrorHandling", Guid.NewGuid().ToString());
        _sourcePath = Path.Combine(_testRootPath, "Source");
        _targetPath = Path.Combine(_testRootPath, "Target");

        Directory.CreateDirectory(_sourcePath);
        Directory.CreateDirectory(_targetPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    [Fact]
    public void SyncResult_Success_ShouldHaveCorrectStatus()
    {
        // Arrange & Act
        var result = SyncResult.Success(5, 2, 1024000, TimeSpan.FromSeconds(1.5));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsPartialSuccess);
        Assert.Equal(5, result.FilesSucceeded);
        Assert.Equal(0, result.FilesFailed);
        Assert.Equal(2, result.FilesDeleted);
        Assert.Equal(1024000, result.TotalBytesCopied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void SyncResult_PartialSuccess_ShouldHaveErrors()
    {
        // Arrange
        var errors = new List<SyncError>
        {
            new SyncError("file1.txt", "アクセス拒否", "UnauthorizedAccessException"),
            new SyncError("file2.txt", "ファイルが見つかりません", "FileNotFoundException")
        };

        // Act
        var result = SyncResult.PartialSuccess(3, 2, 0, 512000, TimeSpan.FromSeconds(2), errors);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsPartialSuccess);
        Assert.Equal(3, result.FilesSucceeded);
        Assert.Equal(2, result.FilesFailed);
        Assert.Equal(5, result.TotalFilesProcessed);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void SyncResult_Failure_ShouldHaveNoSuccesses()
    {
        // Arrange & Act
        var result = SyncResult.Failure("同期失敗: ディレクトリが見つかりません");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsPartialSuccess);
        Assert.Equal(0, result.FilesSucceeded);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void SyncResult_GetSummary_ShouldReturnFormattedString()
    {
        // Arrange
        var errors = new List<SyncError> { new SyncError("test.txt", "エラー", "IOException") };
        var result = SyncResult.PartialSuccess(10, 1, 5, 2048000, TimeSpan.FromSeconds(3.5), errors);

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("部分成功", summary);
        Assert.Contains("10", summary); // 成功数
        Assert.Contains("1", summary);  // 失敗数
        Assert.Contains("5", summary);  // 削除数
        Assert.Contains("MB", summary); // バイト数が変換される
    }

    [Fact]
    public void FileSystemOperations_TryCopyFile_WithReadOnlyTarget_ShouldReturnError()
    {
        // Arrange
        var fileOps = new FileSystemOperations();
        var sourceFile = Path.Combine(_sourcePath, "source.txt");
        var targetFile = Path.Combine(_targetPath, "target.txt");

        File.WriteAllText(sourceFile, "test content");
        File.WriteAllText(targetFile, "old content");
        File.SetAttributes(targetFile, FileAttributes.ReadOnly);

        try
        {
            // Act
            var result = fileOps.TryCopyFile(sourceFile, targetFile);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("UnauthorizedAccessException", result.ExceptionType ?? "");
        }
        finally
        {
            // Cleanup: 読み取り専用属性を解除
            File.SetAttributes(targetFile, FileAttributes.Normal);
        }
    }

    [Fact]
    public void FileSystemOperations_TryDeleteFile_WithNonExistentFile_ShouldSucceed()
    {
        // 存在しないファイルの削除は成功として扱われる（idempotent）
        // Arrange
        var fileOps = new FileSystemOperations();
        var nonExistentFile = Path.Combine(_targetPath, "nonexistent.txt");

        // Act
        var result = fileOps.TryDeleteFile(nonExistentFile);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void FileSystemOperations_TryGetFiles_WithInvalidPath_ShouldReturnError()
    {
        // Arrange
        var fileOps = new FileSystemOperations();
        var invalidPath = Path.Combine(_testRootPath, "NonExistentFolder");

        // Act
        var result = fileOps.TryGetFiles(invalidPath);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Sync_WithPartialFileAccessError_ShouldReturnPartialSuccess()
    {
        // Arrange
        var file1 = Path.Combine(_sourcePath, "file1.txt");
        var file2 = Path.Combine(_sourcePath, "file2.txt");
        var file3 = Path.Combine(_sourcePath, "file3.txt");

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");
        File.WriteAllText(file3, "content3");

        // file2をターゲットに作成し、読み取り専用にする
        var targetFile2 = Path.Combine(_targetPath, "file2.txt");
        File.WriteAllText(targetFile2, "old");
        File.SetAttributes(targetFile2, FileAttributes.ReadOnly);

        try
        {
            // Act
            var result = FolderSyncModule.Library.FolderSyncModule.Sync(
                _sourcePath,
                _targetPath,
                SyncMode.OneWay,
                SyncType.OneTime,
                SyncScope.FileOnly);

            // Assert
            Assert.True(result.IsPartialSuccess);
            Assert.True(result.FilesSucceeded >= 2); // file1とfile3
            Assert.True(result.FilesFailed >= 1);    // file2
            Assert.NotEmpty(result.Errors);

            // エラーメッセージを確認
            var errorForFile2 = result.Errors.FirstOrDefault(e => e.FilePath.Contains("file2.txt"));
            Assert.NotNull(errorForFile2);
        }
        finally
        {
            // Cleanup
            File.SetAttributes(targetFile2, FileAttributes.Normal);
        }
    }

    [Fact]
    public void Sync_WithAllFilesAccessible_ShouldReturnSuccess()
    {
        // Arrange
        var file1 = Path.Combine(_sourcePath, "file1.txt");
        var file2 = Path.Combine(_sourcePath, "file2.txt");

        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");

        // Act
        var result = FolderSyncModule.Library.FolderSyncModule.Sync(
            _sourcePath,
            _targetPath,
            SyncMode.OneWay,
            SyncType.OneTime,
            SyncScope.FileOnly);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.FilesSucceeded);
        Assert.Equal(0, result.FilesFailed);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void OperationResult_Success_ShouldHaveCorrectState()
    {
        // Act
        var result = OperationResult.Success("/path/to/file.txt");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ExceptionType);
        Assert.Equal("/path/to/file.txt", result.TargetPath);
    }

    [Fact]
    public void OperationResult_Failure_ShouldContainErrorInfo()
    {
        // Act
        var result = OperationResult.Failure("エラーメッセージ", "IOException", "/path/to/file.txt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("エラーメッセージ", result.ErrorMessage);
        Assert.Equal("IOException", result.ExceptionType);
        Assert.Equal("/path/to/file.txt", result.TargetPath);
    }

    [Fact]
    public void OperationResultGeneric_Success_ShouldContainValue()
    {
        // Arrange
        var testData = new[] { "file1.txt", "file2.txt" };

        // Act
        var result = OperationResult<string[]>.Success(testData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(testData, result.Value);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void OperationResultGeneric_Failure_ShouldHaveNoValue()
    {
        // Act
        var result = OperationResult<string[]>.Failure("ファイル一覧取得エラー", "DirectoryNotFoundException");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.NotNull(result.ErrorMessage);
    }
}
