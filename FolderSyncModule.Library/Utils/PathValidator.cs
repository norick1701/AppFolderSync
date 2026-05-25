namespace FolderSyncModule.Library.Utils;

/// <summary>
/// パスの検証とセキュリティチェックを行うユーティリティクラス。
/// パストラバーサル攻撃やシンボリックリンクによる循環参照を防ぎます。
/// </summary>
public static class PathValidator
{
    /// <summary>
    /// 指定されたパスが基準ディレクトリ内に存在することを検証します。
    /// パストラバーサル攻撃（../ を使った親ディレクトリへのアクセス）を防ぎます。
    /// </summary>
    /// <param name="basePath">基準となるディレクトリパス</param>
    /// <param name="targetPath">検証対象のパス</param>
    /// <returns>パスが基準ディレクトリ内にある場合は true</returns>
    public static bool IsPathWithinBaseDirectory(string basePath, string targetPath)
    {
        try
        {
            // パスを正規化（相対パス、..、.、末尾のスラッシュなどを解決）
            string normalizedBase = Path.GetFullPath(basePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedTarget = Path.GetFullPath(targetPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // ターゲットが基準ディレクトリ、またはそのサブディレクトリであることを確認
            return normalizedTarget.StartsWith(normalizedBase + Path.DirectorySeparatorChar, 
                StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedBase, normalizedTarget, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // パスの正規化に失敗した場合は安全側に倒して false を返す
            return false;
        }
    }

    /// <summary>
    /// 指定されたパスが基準ディレクトリ内に存在することを検証し、
    /// 違反する場合は例外を投げます。
    /// </summary>
    /// <param name="basePath">基準となるディレクトリパス</param>
    /// <param name="targetPath">検証対象のパス</param>
    /// <exception cref="InvalidOperationException">パストラバーサル攻撃が検出された場合</exception>
    public static void ValidatePathWithinBaseDirectory(string basePath, string targetPath)
    {
        if (!IsPathWithinBaseDirectory(basePath, targetPath))
        {
            throw new InvalidOperationException(
                $"セキュリティ違反: パス '{targetPath}' は基準ディレクトリ '{basePath}' の外部を参照しています。パストラバーサル攻撃の可能性があります。");
        }
    }

    /// <summary>
    /// 相対パスをベースパスと結合し、結果がベースディレクトリ内にあることを保証します。
    /// </summary>
    /// <param name="basePath">基準となるディレクトリパス</param>
    /// <param name="relativePath">相対パス</param>
    /// <returns>安全に結合されたフルパス</returns>
    /// <exception cref="InvalidOperationException">結合後のパスがベースディレクトリ外を指す場合</exception>
    public static string SafeCombine(string basePath, string relativePath)
    {
        // 相対パスに .. が含まれているか確認
        if (relativePath.Contains(".."))
        {
            throw new InvalidOperationException(
                $"セキュリティ違反: 相対パス '{relativePath}' に親ディレクトリ参照 (..) が含まれています。");
        }

        string combinedPath = Path.Combine(basePath, relativePath);
        ValidatePathWithinBaseDirectory(basePath, combinedPath);
        return combinedPath;
    }

    /// <summary>
    /// ファイルまたはディレクトリがシンボリックリンクまたはジャンクションかどうかを判定します。
    /// </summary>
    /// <param name="path">チェック対象のパス</param>
    /// <returns>シンボリックリンクまたはジャンクションの場合は true</returns>
    public static bool IsSymbolicLinkOrJunction(string path)
    {
        try
        {
            // ファイルまたはディレクトリの属性を取得
            FileAttributes attributes = File.GetAttributes(path);

            // ReparsePoint 属性があればシンボリックリンクまたはジャンクション
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch
        {
            // アクセスできない場合は false を返す
            return false;
        }
    }

    /// <summary>
    /// パスの長さがシステムの制限内であることを検証します。
    /// Windowsの標準的な MAX_PATH (260文字) を考慮します。
    /// </summary>
    /// <param name="path">検証対象のパス</param>
    /// <param name="maxLength">最大パス長（デフォルト: 260）</param>
    /// <returns>パスが制限内の場合は true</returns>
    public static bool IsPathLengthValid(string path, int maxLength = 260)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);
            return fullPath.Length < maxLength;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ファイル名に不正な文字が含まれていないかチェックします。
    /// </summary>
    /// <param name="fileName">検証対象のファイル名</param>
    /// <returns>ファイル名が有効な場合は true</returns>
    public static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        char[] invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c));
    }
}
