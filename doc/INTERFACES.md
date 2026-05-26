# FolderSyncModule.Library - インターフェース設計書

## 概要

FolderSyncModule.Library は、DI（依存性注入）対応の設計を採用し、テスト性と拡張性を最大化しています。このドキュメントは、ライブラリが提供するすべてのパブリックインターフェースを詳細に定義します。

---

## インターフェース階層図

```
ISyncStrategy
    ├─ FileOnlySyncStrategy
    ├─ WithDeletionSyncStrategy
    └─ DiffOnlySyncStrategy

IFileSystemOperations
    └─ FileSystemOperations

IDiffDetector
    └─ DiffDetector

IRealtimeSyncWatcher
    └─ RealtimeSyncWatcher
```

---

## 公開インターフェース仕様

### 1. ISyncStrategy

**名前空間**: `FolderSyncModule.Library.Strategies`

**責務**: ファイル同期の戦略を定義

```csharp
public interface ISyncStrategy
{
    /// <summary>ソースディレクトリのファイルをターゲットディレクトリに同期します。</summary>
    void SyncFiles(string sourcePath, string targetPath);

    /// <summary>ターゲットに存在するがソースに存在しないファイルを削除します。</summary>
    void DeleteOrphanedFiles(string sourcePath, string targetPath);
}
```

**実装クラス**:
- `FileOnlySyncStrategy`: ファイルコピーのみ
- `WithDeletionSyncStrategy`: ファイルコピー+削除
- `DiffOnlySyncStrategy`: 差分のみ同期

**使用例**:
```csharp
ISyncStrategy strategy = new DiffOnlySyncStrategy();
strategy.SyncFiles(sourceDir, targetDir);
strategy.DeleteOrphanedFiles(sourceDir, targetDir);
```

---

### 2. IFileSystemOperations

**名前空間**: `FolderSyncModule.Library`

**責務**: ファイルシステム操作の抽象化

```csharp
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
}
```

**実装クラス**:
- `FileSystemOperations`: 標準的なファイルシステム操作

**テスト用モック例**:
```csharp
class MockFileSystemOperations : IFileSystemOperations
{
    public void CopyFile(string sourceFile, string targetFile) 
    { 
        // テスト用の実装
    }
    // ... その他のメソッド
}
```

**DI での使用**:
```csharp
IFileSystemOperations fileOps = new FileSystemOperations();
fileOps.CopyFile("source.txt", "target.txt");
```

---

### 3. IDiffDetector

**名前空間**: `FolderSyncModule.Library`

**責務**: ファイル差分の検出

```csharp
public interface IDiffDetector
{
    /// <summary>ファイルが同期対象かどうかを判定します。</summary>
    bool NeedsSync(string sourceFile, string targetFile);

    /// <summary>ターゲットに存在するがソースに存在しないファイルを取得します。</summary>
    IEnumerable<string> GetOrphanedFiles(string sourcePath, string targetPath);

    /// <summary>ターゲットに存在するがソースに存在しないディレクトリを取得します。</summary>
    IEnumerable<string> GetOrphanedDirectories(string sourcePath, string targetPath);
}
```

**実装クラス**:
- `DiffDetector`: タイムスタンプベースの差分検出

**メソッド詳細**:

#### `NeedsSync()`
- **戻り値**: 同期が必要な場合 true
- **判定ロジック**:
  - ターゲットが存在しない → true
  - ソースが新しい → true
  - その他 → false

#### `GetOrphanedFiles()`
- **戻り値**: ソースに存在しないターゲット内のファイルパス
- **用途**: `WithDeletion` スコープで使用

#### `GetOrphanedDirectories()`
- **戻り値**: ソースに存在しないターゲット内のディレクトリパス
- **用途**: `WithDeletion` スコープで使用

**使用例**:
```csharp
IDiffDetector detector = new DiffDetector();

if (detector.NeedsSync(sourceFile, targetFile))
{
    // ファイルをコピー
}

var orphans = detector.GetOrphanedFiles(sourceDir, targetDir);
foreach (var file in orphans)
{
    // 孤立ファイルを削除
}
```

---

### 4. IRealtimeSyncWatcher

**名前空間**: `FolderSyncModule.Library`

**責務**: ファイルシステム変更の監視

```csharp
public interface IRealtimeSyncWatcher
{
    /// <summary>指定されたフォルダの監視を開始します。</summary>
    void StartWatching(string folderPath);

    /// <summary>監視を停止し、リソースをクリーンアップします。</summary>
    void StopWatching();
}
```

**実装クラス**:
- `RealtimeSyncWatcher`: FileSystemWatcher を使用した監視

**内部動作**:
- FileSystemWatcher でファイル変更を検知
- 500ms の待機で重複呼び出しをブロック
- コールバック経由で呼び出し元に通知

**使用例**:
```csharp
Action<string, string, string, string> onChanged = 
    (path, basePath, action, relativePath) => 
    {
        Console.WriteLine($"[{action}] {relativePath}");
    };

IRealtimeSyncWatcher watcher = new RealtimeSyncWatcher(onChanged);
watcher.StartWatching(folderPath);

// ... ファイル監視中 ...

watcher.StopWatching();
```

---

## DI（依存性注入）パターン

### パターン1: デフォルト実装の使用

```csharp
// 依存関係が自動的に構成される
SyncCoordinator coordinator = new SyncCoordinator();
coordinator.Sync(source, target);
```

### パターン2: カスタム実装の注入

```csharp
// テスト用のモック実装
class MockFileSystemOperations : IFileSystemOperations 
{ 
    // カスタム実装
}

class MockDiffDetector : IDiffDetector 
{ 
    // カスタム実装
}

// モック実装を注入
IFileSystemOperations mockFileOps = new MockFileSystemOperations();
IDiffDetector mockDetector = new MockDiffDetector();

SyncCoordinator coordinator = new SyncCoordinator(mockFileOps, mockDetector);
coordinator.Sync(source, target);
```

### パターン3: DI コンテナを使用（将来対応）

```csharp
var services = new ServiceCollection();
services.AddScoped<IFileSystemOperations, FileSystemOperations>();
services.AddScoped<IDiffDetector, DiffDetector>();
services.AddScoped<SyncCoordinator>();

var provider = services.BuildServiceProvider();
var coordinator = provider.GetRequiredService<SyncCoordinator>();
```

---

## テスト時のモック実装例

### テストシナリオ1: ファイル操作のテスト

```csharp
[Fact]
public void SyncCoordinator_ShouldCopyFiles()
{
    // Arrange
    var mockFileOps = new Mock<IFileSystemOperations>();
    var mockDetector = new Mock<IDiffDetector>();
    
    mockFileOps.Setup(f => f.FileExists(It.IsAny<string>()))
        .Returns(true);
    mockDetector.Setup(d => d.NeedsSync(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(true);
    
    var coordinator = new SyncCoordinator(mockFileOps.Object, mockDetector.Object);
    
    // Act
    coordinator.Sync(source, target, SyncMode.OneWay, SyncType.OneTime, SyncScope.FileOnly);
    
    // Assert
    mockFileOps.Verify(f => f.CopyFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
}
```

### テストシナリオ2: 差分検出のテスト

```csharp
[Fact]
public void DiffDetector_ShouldDetectNewFiles()
{
    // Arrange
    IDiffDetector detector = new DiffDetector();
    
    // Act
    bool result = detector.NeedsSync("new_source.txt", "nonexistent_target.txt");
    
    // Assert
    Assert.True(result);
}
```

---

## 拡張性の考慮

### 新しい同期戦略の追加

```csharp
// 新しい戦略を実装
public class CustomSyncStrategy : ISyncStrategy
{
    public void SyncFiles(string sourcePath, string targetPath)
    {
        // カスタム実装
    }
    
    public void DeleteOrphanedFiles(string sourcePath, string targetPath)
    {
        // カスタム実装
    }
}

// 使用
ISyncStrategy strategy = new CustomSyncStrategy();
```

### 新しいファイル操作の実装

```csharp
// ネットワークドライブ対応の実装
public class NetworkFileSystemOperations : IFileSystemOperations
{
    // ネットワーク経由のファイル操作
}

// DI で注入
var coordinator = new SyncCoordinator(
    new NetworkFileSystemOperations(),
    new DiffDetector()
);
```

---

## インターフェース責務マップ

| インターフェース | 責務 | テスト性 | 拡張性 |
|----------------|------|--------|-------|
| **ISyncStrategy** | 同期戦略定義 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **IFileSystemOperations** | ファイル操作抽象化 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **IDiffDetector** | 差分検出 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **IRealtimeSyncWatcher** | ファイル監視 | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |

---

## 設計パターン

| パターン | 使用箇所 | 目的 |
|---------|---------|------|
| **ストラテジー** | ISyncStrategy | 同期方式の切り替え |
| **ファサード** | FolderSyncModule | 複雑さの隠蔽 |
| **デコレータ** | IFileSystemOperations | ファイル操作の統一 |
| **オブザーバー** | IRealtimeSyncWatcher | ファイル変更通知 |
| **DI** | すべてのインターフェース | テスト性・拡張性向上 |

---

## 使用例：複合シナリオ

```csharp
// シナリオ: カスタムファイル操作 + 差分検出 + 双方向同期

class LoggingFileSystemOperations : IFileSystemOperations
{
    private readonly IFileSystemOperations _inner;
    
    public void CopyFile(string sourceFile, string targetFile)
    {
        Console.WriteLine($"Copying {sourceFile} to {targetFile}");
        _inner.CopyFile(sourceFile, targetFile);
    }
    
    // ... その他のメソッド
}

// 使用
IFileSystemOperations fileOps = new LoggingFileSystemOperations(
    new FileSystemOperations()
);
IDiffDetector detector = new DiffDetector();

SyncCoordinator coordinator = new SyncCoordinator(fileOps, detector);

coordinator.Sync(
    source, 
    target,
    mode: SyncMode.TwoWay,
    syncType: SyncType.OneTime,
    scope: SyncScope.DiffOnly
);
```

---

## ライセンス・バージョン

- **バージョン**: 1.0
- **作成日**: 2026-05-17
- **最終更新**: 2026-05-17
- **ステータス**: DI 対応完了
