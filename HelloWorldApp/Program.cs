using FolderSyncModule.Library;

Console.WriteLine("フォルダ同期ライブラリのデモ");
Console.WriteLine("================================\n");

string sourceFolder = "./SourceFolder";
string targetFolder = "./TargetFolder";

// === 例1: シンプルな同期 ===
Console.WriteLine("【例1: シンプルな同期（デフォルト設定）】");
Console.WriteLine("==================");
CleanupFolders(sourceFolder, targetFolder);
SetupTestFolders(sourceFolder);

Console.WriteLine($"ソース: {sourceFolder}");
Console.WriteLine($"ターゲット: {targetFolder}\n");

FolderSyncModule.Library.FolderSyncModule.Sync(sourceFolder, targetFolder);

Console.WriteLine("\n同期後のターゲット構成:");
PrintFolderStructure(targetFolder);

// === 例2: 削除も含めた完全同期 ===
Console.WriteLine("\n\n【例2: 完全同期（削除も含む）】");
Console.WriteLine("====================");
CleanupFolders(sourceFolder, targetFolder);
SetupTestFolders(sourceFolder);
Directory.CreateDirectory(targetFolder);
File.WriteAllText(Path.Combine(targetFolder, "orphan.txt"), "ターゲット固有のファイル");

Console.WriteLine($"ソース: {sourceFolder}");
Console.WriteLine($"ターゲット: {targetFolder}\n");

FolderSyncModule.Library.FolderSyncModule.Sync(
    sourceFolder,
    targetFolder,
    mode: SyncMode.OneWay,
    syncType: SyncType.OneTime,
    scope: SyncScope.WithDeletion
);

Console.WriteLine("\n同期後のターゲット構成:");
PrintFolderStructure(targetFolder);

// === 例3: 双方向同期 ===
Console.WriteLine("\n\n【例3: 双方向同期】");
Console.WriteLine("==============");
CleanupFolders(sourceFolder, targetFolder);
SetupTestFolders(sourceFolder);
Directory.CreateDirectory(targetFolder);
File.WriteAllText(Path.Combine(targetFolder, "target_file.txt"), "ターゲット固有ファイル");

Console.WriteLine($"ソース: {sourceFolder}");
Console.WriteLine($"ターゲット: {targetFolder}\n");

FolderSyncModule.Library.FolderSyncModule.Sync(
    sourceFolder,
    targetFolder,
    mode: SyncMode.TwoWay,
    syncType: SyncType.OneTime,
    scope: SyncScope.FileOnly
);

Console.WriteLine("\n同期後のソース構成:");
PrintFolderStructure(sourceFolder);

Console.WriteLine("\n同期後のターゲット構成:");
PrintFolderStructure(targetFolder);

static void CleanupFolders(params string[] paths)
{
    foreach (var path in paths)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }
}

static void SetupTestFolders(string sourcePath)
{
    if (Directory.Exists(sourcePath))
        Directory.Delete(sourcePath, true);

    Directory.CreateDirectory(sourcePath);
    File.WriteAllText(Path.Combine(sourcePath, "file1.txt"), "ファイル1");
    File.WriteAllText(Path.Combine(sourcePath, "file2.txt"), "ファイル2");

    string subDir = Path.Combine(sourcePath, "SubFolder");
    Directory.CreateDirectory(subDir);
    File.WriteAllText(Path.Combine(subDir, "file3.txt"), "サブフォルダ内のファイル");
}

static void PrintFolderStructure(string folderPath, string indent = "")
{
    if (!Directory.Exists(folderPath))
        return;

    var files = Directory.GetFiles(folderPath);
    foreach (var file in files)
    {
        Console.WriteLine($"{indent}📄 {Path.GetFileName(file)}");
    }

    var folders = Directory.GetDirectories(folderPath);
    foreach (var folder in folders)
    {
        Console.WriteLine($"{indent}📁 {Path.GetFileName(folder)}/");
        PrintFolderStructure(folder, indent + "  ");
    }
}
