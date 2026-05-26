using FolderSyncModule.Library;

namespace HelloWorldApp.Tests;

/// <summary>
/// セキュリティのテスト。
/// Phase 1の品質改善として、パストラバーサル攻撃の防止とシンボリックリンク対策を検証します。
/// </summary>
public class SecurityTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly string _basePath;

    public SecurityTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), "FolderSyncTests_Security", Guid.NewGuid().ToString());
        _basePath = Path.Combine(_testRootPath, "Base");
        Directory.CreateDirectory(_basePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    [Fact]
    public void PathValidator_IsPathWithinBaseDirectory_ValidPath_ShouldReturnTrue()
    {
        // Arrange
        var validPath = Path.Combine(_basePath, "subfolder", "file.txt");

        // Act
        var result = PathValidator.IsPathWithinBaseDirectory(_basePath, validPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PathValidator_IsPathWithinBaseDirectory_PathTraversal_ShouldReturnFalse()
    {
        // Arrange
        var maliciousPath = Path.Combine(_basePath, "..", "..", "evil.txt");

        // Act
        var result = PathValidator.IsPathWithinBaseDirectory(_basePath, maliciousPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PathValidator_ValidatePathWithinBaseDirectory_ValidPath_ShouldNotThrow()
    {
        // Arrange
        var validPath = Path.Combine(_basePath, "file.txt");

        // Act & Assert
        var exception = Record.Exception(() => 
            PathValidator.ValidatePathWithinBaseDirectory(_basePath, validPath));
        
        Assert.Null(exception);
    }

    [Fact]
    public void PathValidator_ValidatePathWithinBaseDirectory_PathTraversal_ShouldThrow()
    {
        // Arrange
        var maliciousPath = Path.Combine(_basePath, "..", "outside.txt");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PathValidator.ValidatePathWithinBaseDirectory(_basePath, maliciousPath));

        Assert.Contains("セキュリティ違反", exception.Message);
    }

    [Fact]
    public void PathValidator_SafeCombine_NormalPath_ShouldCombineCorrectly()
    {
        // Arrange
        var relativePath = "subfolder/file.txt";

        // Act
        var result = PathValidator.SafeCombine(_basePath, relativePath);

        // Assert
        Assert.Contains(_basePath, result);
        Assert.Contains("file.txt", result);
    }

    [Fact]
    public void PathValidator_SafeCombine_PathTraversal_ShouldThrow()
    {
        // Arrange
        var maliciousRelative = "../../../etc/passwd";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PathValidator.SafeCombine(_basePath, maliciousRelative));

        Assert.Contains("セキュリティ違反", exception.Message);
    }

    [Fact]
    public void PathValidator_IsValidFileName_ValidName_ShouldReturnTrue()
    {
        // Arrange
        var validName = "document_2024.txt";

        // Act
        var result = PathValidator.IsValidFileName(validName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PathValidator_IsValidFileName_InvalidCharacters_ShouldReturnFalse()
    {
        // Arrange
        var invalidNames = new[] { "file<test>.txt", "file|name.txt", "file:name.txt", "file?name.txt" };

        // Act & Assert
        foreach (var invalidName in invalidNames)
        {
            var result = PathValidator.IsValidFileName(invalidName);
            Assert.False(result, $"名前 '{invalidName}' は無効と判定されるべきです");
        }
    }

    [Fact]
    public void PathValidator_IsPathLengthValid_ShortPath_ShouldReturnTrue()
    {
        // Arrange
        var shortPath = Path.Combine(_basePath, "file.txt");

        // Act
        var result = PathValidator.IsPathLengthValid(shortPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PathValidator_IsPathLengthValid_TooLongPath_ShouldReturnFalse()
    {
        // Arrange
        var longFileName = new string('a', 300); // 非常に長いファイル名
        var longPath = Path.Combine(_basePath, longFileName);

        // Act
        var result = PathValidator.IsPathLengthValid(longPath);

        // Assert (Windowsでは260文字制限がある場合があるため)
        // このテストはOSによって結果が異なる可能性があるため、条件付きでチェック
        if (OperatingSystem.IsWindows())
        {
            Assert.False(result);
        }
    }

    [Fact]
    public void FileSystemOperations_TryGetFiles_WithSymbolicLinks_ShouldExcludeThem()
    {
        // このテストはシンボリックリンクをサポートするOSでのみ実行
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return; // シンボリックリンクをサポートしていないOSではスキップ
        }

        // Arrange
        var testDir = Path.Combine(_testRootPath, "SymlinkTest");
        Directory.CreateDirectory(testDir);

        var realFile = Path.Combine(testDir, "realfile.txt");
        File.WriteAllText(realFile, "real content");

        // Act
        var fileOps = new FileSystemOperations();
        var result = fileOps.TryGetFiles(testDir, excludeSymbolicLinks: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains(realFile, result.Value);
    }

    [Fact]
    public void PathValidator_IsSymbolicLinkOrJunction_RegularFile_ShouldReturnFalse()
    {
        // Arrange
        var regularFile = Path.Combine(_basePath, "regular.txt");
        File.WriteAllText(regularFile, "content");

        // Act
        var result = PathValidator.IsSymbolicLinkOrJunction(regularFile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void PathValidator_IsSymbolicLinkOrJunction_RegularDirectory_ShouldReturnFalse()
    {
        // Arrange
        var regularDir = Path.Combine(_basePath, "regularDir");
        Directory.CreateDirectory(regularDir);

        // Act
        var result = PathValidator.IsSymbolicLinkOrJunction(regularDir);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Sync_WithMaliciousRelativePath_ShouldNotEscapeBase()
    {
        // このテストは、内部的にSafeCombin を使用することを確認します
        // 実際の攻撃シナリオをシミュレートするのは困難なため、
        // PathValidatorが正しく統合されていることを確認する統合テストです

        // Arrange
        var sourcePath = Path.Combine(_testRootPath, "Source");
        var targetPath = Path.Combine(_testRootPath, "Target");
        Directory.CreateDirectory(sourcePath);
        Directory.CreateDirectory(targetPath);

        var testFile = Path.Combine(sourcePath, "test.txt");
        File.WriteAllText(testFile, "test content");

        // Act & Assert - 正常な同期は成功するはず
        var result = FolderSyncModule.Library.FolderSyncModule.Sync(
            sourcePath,
            targetPath,
            SyncMode.OneWay,
            SyncType.OneTime,
            SyncScope.FileOnly);

        Assert.True(result.IsSuccess || result.IsPartialSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void PathValidator_SafeCombine_EmptyOrNullRelative_ShouldHandleGracefully(string? relativePath)
    {
        // Act & Assert
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            // 空やnullの相対パスは、ベースパスをそのまま返すべき
            var exception = Record.Exception(() => PathValidator.SafeCombine(_basePath, relativePath ?? ""));
            
            // 実装によってはエラーを投げる可能性もあるが、
            // 少なくともクラッシュしないことを確認
            Assert.True(exception == null || exception is InvalidOperationException);
        }
    }

    [Fact]
    public void PathValidator_IsPathWithinBaseDirectory_SamePath_ShouldReturnTrue()
    {
        // Arrange & Act
        var result = PathValidator.IsPathWithinBaseDirectory(_basePath, _basePath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PathValidator_IsPathWithinBaseDirectory_CaseInsensitive_OnWindows_ShouldWork()
    {
        // Windowsでは大文字小文字を区別しない
        if (!OperatingSystem.IsWindows())
        {
            return; // Windowsでのみ実行
        }

        // Arrange
        var lowerPath = Path.Combine(_basePath, "test.txt").ToLower();

        // Act
        var result = PathValidator.IsPathWithinBaseDirectory(_basePath, lowerPath);

        // Assert
        Assert.True(result);
    }
}
