using FolderSyncModule.Library.Models;
using FolderSyncModule.Library.Utils;

namespace FolderSyncModule.Library;

/// <summary>
/// ファイルシステムの操作を担当するクラス。
/// ファイルやディレクトリの作成、コピー、削除などの基本操作を提供します。
/// テスト時に容易にモック化できるように設計されています。
/// </summary>
public class FileSystemOperations : IFileSystemOperations
{
    /// <summary>
    /// ファイルをコピーします。
    /// ターゲットディレクトリが存在しない場合は自動的に作成します。
    /// </summary>
    public void CopyFile(string sourceFile, string targetFile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
        File.Copy(sourceFile, targetFile, overwrite: true);
    }

    /// <summary>
    /// ディレクトリを作成します。
    /// 親ディレクトリが存在しない場合も自動的に作成されます。
    /// </summary>
    public void CreateDirectory(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// ファイルを削除します。
    /// ファイルが存在しない場合はエラーにならず処理を続行します。
    /// </summary>
    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    /// <summary>
    /// ディレクトリをその中身ごと削除します。
    /// ディレクトリが存在しない場合はエラーにならず処理を続行します。
    /// </summary>
    public void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            Directory.Delete(directoryPath, true);
    }

    /// <summary>
    /// 指定されたパスのすべてのファイルを取得します。
    /// </summary>
    public string[] GetFiles(string path) => Directory.GetFiles(path);

    /// <summary>
    /// 指定されたパスのすべてのディレクトリを取得します。
    /// </summary>
    public string[] GetDirectories(string path) => Directory.GetDirectories(path);

    /// <summary>
    /// ファイルが存在するかを確認します。
    /// </summary>
    public bool FileExists(string path) => File.Exists(path);

    /// <summary>
    /// ディレクトリが存在するかを確認します。
    /// </summary>
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <summary>
    /// ファイルサイズを取得します。
    /// </summary>
    public long GetFileSize(string filePath)
    {
        try
        {
            return new FileInfo(filePath).Length;
        }
        catch
        {
            return 0;
        }
    }

    // 例外安全なメソッド（エラーを Result 型で返す）

    /// <summary>
    /// ファイルコピーのバッファサイズ（1MB）。
    /// 大きなバッファサイズにより、特に大きなファイルのコピーが高速化されます。
    /// </summary>
    private const int BufferSize = 1024 * 1024; // 1MB

    /// <summary>
    /// ファイルを安全にコピーします。失敗時は Result でエラー情報を返します。
    /// 大きなバッファサイズ（1MB）を使用して高速化しています。
    /// </summary>
    public OperationResult TryCopyFile(string sourceFile, string targetFile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            
            // 最適化されたバッファサイズでコピー
            using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
            using var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan);
            sourceStream.CopyTo(targetStream, BufferSize);
            
            return OperationResult.Success(targetFile);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure(
                $"アクセスが拒否されました: {ex.Message}",
                nameof(UnauthorizedAccessException),
                targetFile);
        }
        catch (PathTooLongException ex)
        {
            return OperationResult.Failure(
                $"パスが長すぎます: {ex.Message}",
                nameof(PathTooLongException),
                targetFile);
        }
        catch (NotSupportedException ex)
        {
            return OperationResult.Failure(
                $"サポートされていない操作です: {ex.Message}",
                nameof(NotSupportedException),
                targetFile);
        }
        catch (IOException ex)
        {
            return OperationResult.Failure(
                $"ファイルコピーに失敗しました: {ex.Message}",
                nameof(IOException),
                targetFile);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(
                $"予期しないエラー: {ex.Message}",
                ex.GetType().Name,
                targetFile);
        }
    }

    /// <summary>
    /// ファイルを安全に削除します。失敗時は Result でエラー情報を返します。
    /// </summary>
    public OperationResult TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return OperationResult.Success(filePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure(
                $"アクセスが拒否されました: {ex.Message}",
                nameof(UnauthorizedAccessException),
                filePath);
        }
        catch (IOException ex)
        {
            return OperationResult.Failure(
                $"ファイル削除に失敗しました: {ex.Message}",
                nameof(IOException),
                filePath);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(
                $"予期しないエラー: {ex.Message}",
                ex.GetType().Name,
                filePath);
        }
    }

    /// <summary>
    /// ディレクトリを安全に削除します。失敗時は Result でエラー情報を返します。
    /// </summary>
    public OperationResult TryDeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
            return OperationResult.Success(directoryPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure(
                $"アクセスが拒否されました: {ex.Message}",
                nameof(UnauthorizedAccessException),
                directoryPath);
        }
        catch (IOException ex)
        {
            return OperationResult.Failure(
                $"ディレクトリ削除に失敗しました: {ex.Message}",
                nameof(IOException),
                directoryPath);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure(
                $"予期しないエラー: {ex.Message}",
                ex.GetType().Name,
                directoryPath);
        }
    }

    /// <summary>
    /// 指定されたパスのすべてのファイルを取得します（シンボリックリンク除外オプション付き）。
    /// </summary>
    public OperationResult<string[]> TryGetFiles(string path, bool excludeSymbolicLinks = true)
    {
        try
        {
            EnumerationOptions options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                AttributesToSkip = excludeSymbolicLinks ? FileAttributes.ReparsePoint : 0
            };

            string[] files = Directory.GetFiles(path, "*", options);
            return OperationResult<string[]>.Success(files);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<string[]>.Failure(
                $"アクセスが拒否されました: {ex.Message}",
                nameof(UnauthorizedAccessException));
        }
        catch (DirectoryNotFoundException ex)
        {
            return OperationResult<string[]>.Failure(
                $"ディレクトリが見つかりません: {ex.Message}",
                nameof(DirectoryNotFoundException));
        }
        catch (Exception ex)
        {
            return OperationResult<string[]>.Failure(
                $"予期しないエラー: {ex.Message}",
                ex.GetType().Name);
        }
    }

    /// <summary>
    /// 指定されたパスのすべてのディレクトリを取得します（シンボリックリンク除外オプション付き）。
    /// </summary>
    public OperationResult<string[]> TryGetDirectories(string path, bool excludeSymbolicLinks = true)
    {
        try
        {
            EnumerationOptions options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                AttributesToSkip = excludeSymbolicLinks ? FileAttributes.ReparsePoint : 0
            };

            string[] directories = Directory.GetDirectories(path, "*", options);
            return OperationResult<string[]>.Success(directories);
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<string[]>.Failure(
                $"アクセスが拒否されました: {ex.Message}",
                nameof(UnauthorizedAccessException));
        }
        catch (DirectoryNotFoundException ex)
        {
            return OperationResult<string[]>.Failure(
                $"ディレクトリが見つかりません: {ex.Message}",
                nameof(DirectoryNotFoundException));
        }
        catch (Exception ex)
        {
            return OperationResult<string[]>.Failure(
                $"予期しないエラー: {ex.Message}",
                ex.GetType().Name);
        }
    }
}

