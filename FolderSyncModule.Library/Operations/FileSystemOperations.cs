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
}

