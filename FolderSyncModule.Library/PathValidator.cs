namespace FolderSyncModule.Library;

/// <summary>
/// パスの検証とセキュリティチェックを行うユーティリティクラス。
/// パストラバーサル攻撃やシンボリックリンクによる循環参照を防ぎます。
/// </summary>
public static class PathValidator
{
    /// <summary>
    /// 指定されたパスが基準ディレクトリ内に存在することを検証します。
    /// </summary>
    public static bool IsPathWithinBaseDirectory(string basePath, string targetPath)
    {
        try
        {
            string normalizedBase = Path.GetFullPath(basePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedTarget = Path.GetFullPath(targetPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalizedTarget.StartsWith(normalizedBase + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedBase, normalizedTarget, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 指定されたパスが基準ディレクトリ内に存在することを検証し、違反する場合は例外を投げます。
    /// </summary>
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
    public static string SafeCombine(string basePath, string relativePath)
    {
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
    public static bool IsSymbolicLinkOrJunction(string path)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// パスの長さがシステムの制限内であることを検証します。
    /// </summary>
    public static bool IsPathLengthValid(string path, int maxLength = 260)
    {
        try
        {
            return Path.GetFullPath(path).Length < maxLength;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ファイル名に不正な文字が含まれていないかチェックします。
    /// </summary>
    public static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        char[] invalidChars = Path.GetInvalidFileNameChars();
        return !fileName.Any(c => invalidChars.Contains(c));
    }
}
