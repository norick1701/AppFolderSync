using FolderSyncModule.Library.Models;

namespace FolderSyncModule.Library;

/// <summary>
/// ファイルシステムの操作をカプセル化するインターフェース。
/// テスト時にモック化可能にするために設計されています。
/// </summary>
public interface IFileSystemOperations
{
    void CopyFile(string sourceFile, string targetFile);
    void CreateDirectory(string directoryPath);
    void DeleteFile(string filePath);
    void DeleteDirectory(string directoryPath);
    string[] GetFiles(string path);
    string[] GetDirectories(string path);
    bool FileExists(string path);
    bool DirectoryExists(string path);
    OperationResult TryCopyFile(string sourceFile, string targetFile);
    OperationResult TryDeleteFile(string filePath);
    OperationResult TryDeleteDirectory(string directoryPath);
    OperationResult<string[]> TryGetFiles(string path, bool excludeSymbolicLinks = true);
    OperationResult<string[]> TryGetDirectories(string path, bool excludeSymbolicLinks = true);
    long GetFileSize(string filePath);
}

/// <summary>
/// ファイルシステムの操作を担当するクラス。
/// </summary>
public class FileSystemOperations : IFileSystemOperations
{
    private const int BufferSize = 1024 * 1024; // 1MB

    public void CopyFile(string sourceFile, string targetFile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
        File.Copy(sourceFile, targetFile, overwrite: true);
    }

    public void CreateDirectory(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            Directory.Delete(directoryPath, true);
    }

    public string[] GetFiles(string path) => Directory.GetFiles(path);

    public string[] GetDirectories(string path) => Directory.GetDirectories(path);

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public long GetFileSize(string filePath)
    {
        try { return new FileInfo(filePath).Length; }
        catch { return 0; }
    }

    public OperationResult TryCopyFile(string sourceFile, string targetFile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
            using var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan);
            sourceStream.CopyTo(targetStream, BufferSize);
            return OperationResult.Success(targetFile);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure($"アクセスが拒否されました: {ex.Message}", nameof(UnauthorizedAccessException), targetFile);
        }
        catch (PathTooLongException ex)
        {
            return OperationResult.Failure($"パスが長すぎます: {ex.Message}", nameof(PathTooLongException), targetFile);
        }
        catch (NotSupportedException ex)
        {
            return OperationResult.Failure($"サポートされていない操作です: {ex.Message}", nameof(NotSupportedException), targetFile);
        }
        catch (IOException ex)
        {
            return OperationResult.Failure($"ファイルコピーに失敗しました: {ex.Message}", nameof(IOException), targetFile);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"予期しないエラー: {ex.Message}", ex.GetType().Name, targetFile);
        }
    }

    public OperationResult TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            return OperationResult.Success(filePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure($"アクセスが拒否されました: {ex.Message}", nameof(UnauthorizedAccessException), filePath);
        }
        catch (IOException ex)
        {
            return OperationResult.Failure($"ファイル削除に失敗しました: {ex.Message}", nameof(IOException), filePath);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"予期しないエラー: {ex.Message}", ex.GetType().Name, filePath);
        }
    }

    public OperationResult TryDeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
            return OperationResult.Success(directoryPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure($"アクセスが拒否されました: {ex.Message}", nameof(UnauthorizedAccessException), directoryPath);
        }
        catch (IOException ex)
        {
            return OperationResult.Failure($"ディレクトリ削除に失敗しました: {ex.Message}", nameof(IOException), directoryPath);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"予期しないエラー: {ex.Message}", ex.GetType().Name, directoryPath);
        }
    }

    public OperationResult<string[]> TryGetFiles(string path, bool excludeSymbolicLinks = true)
    {
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                AttributesToSkip = excludeSymbolicLinks ? FileAttributes.ReparsePoint : 0
            };
            return OperationResult<string[]>.Success(Directory.GetFiles(path, "*", options));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<string[]>.Failure($"アクセスが拒否されました: {ex.Message}", nameof(UnauthorizedAccessException));
        }
        catch (DirectoryNotFoundException ex)
        {
            return OperationResult<string[]>.Failure($"ディレクトリが見つかりません: {ex.Message}", nameof(DirectoryNotFoundException));
        }
        catch (Exception ex)
        {
            return OperationResult<string[]>.Failure($"予期しないエラー: {ex.Message}", ex.GetType().Name);
        }
    }

    public OperationResult<string[]> TryGetDirectories(string path, bool excludeSymbolicLinks = true)
    {
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                AttributesToSkip = excludeSymbolicLinks ? FileAttributes.ReparsePoint : 0
            };
            return OperationResult<string[]>.Success(Directory.GetDirectories(path, "*", options));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<string[]>.Failure($"アクセスが拒否されました: {ex.Message}", nameof(UnauthorizedAccessException));
        }
        catch (DirectoryNotFoundException ex)
        {
            return OperationResult<string[]>.Failure($"ディレクトリが見つかりません: {ex.Message}", nameof(DirectoryNotFoundException));
        }
        catch (Exception ex)
        {
            return OperationResult<string[]>.Failure($"予期しないエラー: {ex.Message}", ex.GetType().Name);
        }
    }
}
