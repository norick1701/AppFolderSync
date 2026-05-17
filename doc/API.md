# FolderSyncModule - API仕様書

## 概要

FolderSyncModule は、フォルダ間でのファイル同期機能を提供する .NET ライブラリです。このドキュメントは、モジュールの完全なAPI仕様を定義します。

---

## 公開API

### 1. FolderSyncModule クラス

#### 説明
外部から使用する唯一のパブリッククラス。ファサードパターンで複雑な内部実装を隠蔽します。

#### 静的メソッド

##### `Sync()`

**メソッド署名**
```csharp
public static void Sync(
    string sourcePath,
    string targetPath,
    SyncMode mode = SyncMode.OneWay,
    SyncType syncType = SyncType.OneTime,
    SyncScope scope = SyncScope.DiffOnly
)
```

**説明**
フォルダの同期を実行します。指定された設定に従い、ワンタイム同期またはリアルタイム監視を開始します。

**パラメータ**

| パラメータ | 型 | デフォルト | 説明 |
|-----------|----|---------|----|
| `sourcePath` | string | 必須 | 同期元ディレクトリパス |
| `targetPath` | string | 必須 | 同期先ディレクトリパス |
| `mode` | SyncMode | OneWay | 同期モード（単向/双方向） |
| `syncType` | SyncType | OneTime | 同期タイプ（ワンタイム/リアルタイム） |
| `scope` | SyncScope | DiffOnly | 同期範囲（コピーのみ/削除も含む/差分のみ） |

**戻り値**
void

**例外**

| 例外 | 条件 |
|------|------|
| `DirectoryNotFoundException` | sourcePath が存在しない場合 |
| `UnauthorizedAccessException` | ファイル操作権限がない場合 |
| `IOException` | ファイルロック中など、I/Oエラー発生時 |

**使用例**
```csharp
// 基本的な使用
FolderSyncModule.Sync("C:/Source", "C:/Target");

// カスタム設定
FolderSyncModule.Sync(
    "C:/Source",
    "C:/Target",
    mode: SyncMode.TwoWay,
    syncType: SyncType.Realtime,
    scope: SyncScope.WithDeletion
);
```

---

##### `StopRealtimeSync()`

**メソッド署名**
```csharp
public static void StopRealtimeSync()
```

**説明**
リアルタイム監視を停止します。`SyncType.Realtime` で同期している場合のみ呼び出してください。

**パラメータ**
なし

**戻り値**
void

**例外**
- リアルタイム監視中でない場合は例外は発生しません

**使用例**
```csharp
FolderSyncModule.Sync(
    "C:/Source",
    "C:/Target",
    syncType: SyncType.Realtime
);

Console.WriteLine("監視中...");
Console.ReadLine();

FolderSyncModule.StopRealtimeSync();
```

---

### 2. Enum 定義

#### SyncMode

```csharp
public enum SyncMode
{
    /// <summary>ソース → ターゲットの一方向同期</summary>
    OneWay = 0,
    
    /// <summary>ソース ↔ ターゲットの双方向同期</summary>
    TwoWay = 1
}
```

**説明**
同期の方向を指定します。

| 値 | 説明 |
|----|------|
| `OneWay` | ソースをターゲットに一方向でコピー |
| `TwoWay` | ソースとターゲットを相互に同期 |

---

#### SyncType

```csharp
public enum SyncType
{
    /// <summary>一度だけ実行される同期</summary>
    OneTime = 0,
    
    /// <summary>ファイル変更を監視して継続的に同期</summary>
    Realtime = 1
}
```

**説明**
同期の実行方法を指定します。

| 値 | 説明 |
|----|------|
| `OneTime` | メソッド呼び出し時に一度だけ同期。処理完了後は終了 |
| `Realtime` | ファイル変更を監視して自動同期。`StopRealtimeSync()` で停止 |

---

#### SyncScope

```csharp
public enum SyncScope
{
    /// <summary>ファイルコピーのみ。ターゲット固有のファイルは削除されない</summary>
    FileOnly = 0,
    
    /// <summary>ファイルコピーと削除の両方。ターゲットをソースと完全に一致させる</summary>
    WithDeletion = 1,
    
    /// <summary>差分のみ同期。新規または更新されたファイルのみコピー</summary>
    DiffOnly = 2
}
```

**説明**
同期の範囲と処理内容を指定します。

| 値 | 説明 | 使用例 |
|----|------|-------|
| `FileOnly` | ソースのファイルをすべてコピー。ターゲット固有ファイルは保持 | ターゲットに独自ファイルがある場合 |
| `WithDeletion` | ソースのファイルをコピーし、ターゲット固有ファイルを削除 | ターゲットをソースと完全一致させたい場合 |
| `DiffOnly` | ソースにない、または古いファイルのみコピー。タイムスタンプ比較で判定 | 大量ファイルで効率重視 |

---

## 使用パターン

### パターン1: シンプルなバックアップ

```csharp
// データをバックアップディレクトリにコピー
FolderSyncModule.Sync(
    sourcePath: "C:/ImportantData",
    targetPath: "D:/Backup"
);
```

**動作**
- ソースのすべてのファイルをターゲットにコピー
- タイムスタンプが古いファイルはスキップ
- ターゲット固有ファイルは保持

---

### パターン2: 完全ミラーリング

```csharp
// ターゲットをソースの完全なコピーにする
FolderSyncModule.Sync(
    sourcePath: "C:/Master",
    targetPath: "C:/Replica",
    scope: SyncScope.WithDeletion
);
```

**動作**
- ソースのすべてのファイルをコピー
- ターゲット固有ファイルを削除
- ソースと完全に一致

---

### パターン3: リアルタイム同期

```csharp
// ファイル変更を自動的に同期
FolderSyncModule.Sync(
    sourcePath: "C:/Source",
    targetPath: "C:/Target",
    syncType: SyncType.Realtime
);

// ファイル変更を自動監視...
Console.WriteLine("監視中 (Ctrl+C で終了)");
Console.ReadLine();

// 監視を停止
FolderSyncModule.StopRealtimeSync();
```

**動作**
- 初回はワンタイム同期を実行
- その後、ソースのファイル変更を監視
- 作成・更新・削除時に自動的にターゲットに反映
- `StopRealtimeSync()` 呼び出しで監視を終了

---

### パターン4: 双方向同期

```csharp
// 2つのフォルダを相互同期
FolderSyncModule.Sync(
    sourcePath: "C:/FolderA",
    targetPath: "C:/FolderB",
    mode: SyncMode.TwoWay,
    scope: SyncScope.DiffOnly
);
```

**動作**
- Phase 1: ソース → ターゲット (差分のみ)
- Phase 2: ターゲット → ソース (差分のみ)
- 結果: 両フォルダが同じ内容に

---

### パターン5: 高速差分同期

```csharp
// 大量ファイル。差分のみで高速化
FolderSyncModule.Sync(
    sourcePath: "C:/LargeFolder",
    targetPath: "C:/Backup",
    scope: SyncScope.DiffOnly
);
```

**動作**
- タイムスタンプ比較で差分を検出
- 新規または更新ファイルのみコピー
- スキップされたファイルはログに記録

---

## エラー処理ガイド

### DirectoryNotFoundException

```csharp
try
{
    FolderSyncModule.Sync(sourcePath, targetPath);
}
catch (DirectoryNotFoundException ex)
{
    Console.WriteLine($"エラー: ソースディレクトリが見つかりません");
    Console.WriteLine($"パス: {sourcePath}");
    // リトライ、代替パス使用、ユーザー通知など
}
```

**対応**
- ソースパスの存在確認
- タイプミスのチェック
- パーミッション確認

---

### UnauthorizedAccessException

```csharp
try
{
    FolderSyncModule.Sync(sourcePath, targetPath);
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"エラー: ファイルアクセス権限がありません");
    // 管理者実行、パーミッション設定の見直し
}
```

**対応**
- 管理者権限で実行
- ファイル・フォルダのパーミッション確認
- ウイルス対策ソフトの干渉確認

---

### IOException

```csharp
try
{
    FolderSyncModule.Sync(sourcePath, targetPath);
}
catch (IOException ex)
{
    Console.WriteLine($"エラー: ファイルI/Oエラーが発生しました");
    // ファイルロック、ディスク容量、USB接続確認
    
    // リトライ
    System.Threading.Thread.Sleep(1000);
    FolderSyncModule.Sync(sourcePath, targetPath);
}
```

**対応**
- ファイルロックの解放
- ディスク容量確認
- USB接続、ネットワーク接続確認

---

## パフォーマンス考慮事項

### 最適化のヒント

**大容量ファイルの場合**
```csharp
// 差分のみ同期で不要なコピーを避ける
FolderSyncModule.Sync(
    sourcePath,
    targetPath,
    scope: SyncScope.DiffOnly
);
```

**小ファイル多数の場合**
```csharp
// 全コピーで一括処理（バッチ化）
FolderSyncModule.Sync(
    sourcePath,
    targetPath,
    scope: SyncScope.FileOnly
);
```

**リアルタイム監視の場合**
```csharp
// 負荷軽減のため、差分のみに設定
FolderSyncModule.Sync(
    sourcePath,
    targetPath,
    syncType: SyncType.Realtime,
    scope: SyncScope.DiffOnly
);
```

---

## 制限事項

| 項目 | 制限 | 説明 |
|------|------|------|
| **パス長** | 260文字 | Windows の制限に準拠 |
| **ファイルサイズ** | 制限なし | ストリーミングで対応 |
| **同時監視数** | OSに依存 | FileSystemWatcher の制限 |
| **リアルタイム遅延** | ~500ms | OS通知待機時間 |

---

## バージョン情報

- **API バージョン**: 1.0
- **.NET バージョン**: 10.0
- **最終更新**: 2026-05-17

## サポート

問題が発生した場合:
1. README.md を確認
2. DESIGN.md でアーキテクチャを確認
3. テストコードで使用例を確認
4. ログメッセージを確認
