# フォルダ同期モジュール - 設計書

## 1. 概要

### 目的
ソースディレクトリをターゲットディレクトリに同期するモジュール。複数の同期モード・タイプ・スコープをサポートし、柔軟なファイル同期機能を提供します。

### 主な機能
- **同期モード**: 単向同期（ソース→ターゲット）、双方向同期（相互同期）
- **同期タイプ**: ワンタイム同期、リアルタイム監視
- **同期範囲**: ファイルコピーのみ、削除も含む、差分のみ
- **再帰処理**: サブディレクトリの自動同期

---

## 2. アーキテクチャ

### 2.1 全体構成図

```
┌─────────────────────────────────────┐
│   FolderSyncModule (API)            │ ← 外部インターフェース
│   (ファサード)                       │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│   SyncCoordinator                   │ ← 全体調整
│   (ファサード・コーディネータ)       │
└─┬───────────┬──────────────┬────────┘
  │           │              │
  ▼           ▼              ▼
┌────────────┐ ┌──────────┐ ┌──────────────┐
│ Strategy   │ │FileSystem│ │  DiffDetector│
│(ファイル   │ │Ops       │ │ (差分検出)   │
│ 同期戦略)  │ └──────────┘ └──────────────┘
└────────────┘
  ▲ ▲ ▲
  │ │ │
┌─┴─┴─┴────────────────┐
│FileOnly              │
│WithDeletion          │
│DiffOnly              │
└──────────────────────┘

┌──────────────────────┐
│RealtimeSyncWatcher   │ ← リアルタイム監視
│(ファイル監視)        │
└──────────────────────┘
```

### 2.2 設計パターン

| パターン | 使用箇所 | 目的 |
|---------|---------|------|
| **ファサード** | FolderSyncModule, SyncCoordinator | 複雑な内部ロジックを隠蔽 |
| **ストラテジー** | ISyncStrategy | 同期スコープに応じた処理切り替え |
| **オブザーバー** | RealtimeSyncWatcher | ファイル変更の検知 |
| **デコレータ** | FileSystemOperations | ファイル操作の統一インターフェース |

---

## 3. クラス設計

### 3.1 責務の分割

```
FolderSyncModule (外部API)
    ├─ SyncCoordinator (全体調整)
    │   ├─ FileSystemOperations (ファイル操作)
    │   ├─ DiffDetector (差分検出)
    │   ├─ ISyncStrategy (同期戦略)
    │   │   ├─ FileOnlySyncStrategy
    │   │   ├─ WithDeletionSyncStrategy
    │   │   └─ DiffOnlySyncStrategy
    │   └─ RealtimeSyncWatcher (監視)
    └─ Enums (設定値)
        ├─ SyncMode
        ├─ SyncType
        └─ SyncScope
```

### 3.2 クラス詳細

#### **FolderSyncModule**
- **責務**: 外部への統一インターフェース提供
- **メソッド**:
  - `Sync()`: 同期実行（ファサード）
  - `StopRealtimeSync()`: 監視停止

#### **SyncCoordinator**
- **責務**: 各コンポーネントの調整・オーケストレーション
- **主要メソッド**:
  - `Sync()`: 全体的な同期処理
  - `PerformSync()`: ワンタイム同期実行
  - `StartRealtimeSync()`: リアルタイム監視開始
  - `OnSourceChanged()`: ソース変更時処理
  - `OnTargetChanged()`: ターゲット変更時処理

#### **FileSystemOperations**
- **責務**: ファイル・ディレクトリ操作の統一インターフェース
- **主要メソッド**:
  - `CopyFile()`: ファイルコピー
  - `CreateDirectory()`: ディレクトリ作成
  - `DeleteFile()`: ファイル削除
  - `DeleteDirectory()`: ディレクトリ削除
  - `FileExists()`, `DirectoryExists()`: 存在確認

#### **DiffDetector**
- **責務**: ファイル差分検出
- **主要メソッド**:
  - `NeedsSync()`: 同期必要性判定
  - `GetOrphanedFiles()`: 孤立ファイル検出
  - `GetOrphanedDirectories()`: 孤立ディレクトリ検出

#### **ISyncStrategy (インターフェース)**
- **責務**: 同期スコープの戦略定義
- **実装クラス**:
  - `FileOnlySyncStrategy`: ファイルコピーのみ
  - `WithDeletionSyncStrategy`: コピー+削除
  - `DiffOnlySyncStrategy`: 差分のみ

#### **RealtimeSyncWatcher**
- **責務**: ファイルシステムの変更監視
- **主要メソッド**:
  - `StartWatching()`: 監視開始
  - `StopWatching()`: 監視停止
  - `OnChanged()`: 変更検知処理

---

## 4. データフロー

### 4.1 ワンタイム同期の流れ

```
FolderSyncModule.Sync()
    ↓
SyncCoordinator.Sync()
    ↓
PerformSync()
    ├─ SyncDirectoriesRecursive() ... サブフォルダ作成
    ├─ Strategy.SyncFiles() ... ファイルコピー
    └─ Strategy.DeleteOrphanedFiles() ... 孤立ファイル削除
    ↓
✓ 同期完了
```

### 4.2 リアルタイム監視の流れ

```
FolderSyncModule.Sync(syncType=Realtime)
    ↓
SyncCoordinator.Sync()
    ├─ PerformSync() ... 初期同期実行
    └─ StartRealtimeSync()
        ├─ RealtimeSyncWatcher (ソース監視)
        └─ RealtimeSyncWatcher (ターゲット監視)
        ↓
        ◄ ファイル変更検知 ◄
        ↓
    OnSourceChanged() / OnTargetChanged()
        ├─ FileSystemOperations ... ファイル操作
        └─ 変更をターゲット/ソースに反映
        ↓
    (ファイル変更がある限り繰り返し)
```

---

## 5. 同期ロジック

### 5.1 SyncScope別の処理

| SyncScope | ファイルコピー | 孤立ファイル削除 | 新規ファイルチェック | 用途 |
|-----------|---------------|-----------------|------------------|------|
| **FileOnly** | ✓ 全て | ✗ 削除なし | ✓ 全て | ターゲット固有ファイルを保持 |
| **WithDeletion** | ✓ 全て | ✓ 削除 | ✓ 全て | ターゲットをソースと完全一致 |
| **DiffOnly** | ✓ 差分のみ | ✗ 削除なし | ✓ タイムスタンプ比較 | パフォーマンス重視 |

### 5.2 差分検出アルゴリズム (DiffOnly)

```
for each sourceFile in sourcePath:
    targetFile = targetPath + sourceFile.name
    
    if targetFile does not exist:
        action = COPY
    else:
        sourceTime = sourceFile.LastWriteTime
        targetTime = targetFile.LastWriteTime
        
        if sourceTime > targetTime:
            action = COPY
        else:
            action = SKIP
    
    execute(action)
```

### 5.3 双方向同期 (TwoWay)

```
Phase 1: ソース → ターゲット
    - ソースのファイルをターゲットにコピー
    - ターゲット固有ファイルは削除（WithDeletion時）

Phase 2: ターゲット → ソース
    - ターゲットのファイルをソースにコピー
    - ソース固有ファイルは削除（WithDeletion時）

結果: 両ディレクトリが同期
```

---

## 6. ディレクトリ構成

```
HelloWorldApp/
├── FolderSyncModule.cs          ← 外部API (ファサード)
├── Strategies/                  ← 同期戦略
│   ├── ISyncStrategy.cs
│   ├── FileOnlySyncStrategy.cs
│   ├── WithDeletionSyncStrategy.cs
│   └── DiffOnlySyncStrategy.cs
├── Operations/                  ← ファイル操作
│   └── FileSystemOperations.cs
├── Detection/                   ← 差分検出
│   └── DiffDetector.cs
├── Watchers/                    ← ファイル監視
│   └── RealtimeSyncWatcher.cs
└── Coordinators/                ← 全体調整
    └── SyncCoordinator.cs

HelloWorldApp.Tests/
├── FileSystemOperationsTests.cs
├── DiffDetectorTests.cs
├── SyncStrategyTests.cs
└── SyncCoordinatorIntegrationTests.cs
```

---

## 7. 使用例

### 7.1 シンプルな同期

```csharp
// デフォルト: 単向、ワンタイム、差分のみ
FolderSyncModule.Sync("C:/Source", "C:/Target");
```

### 7.2 完全同期（削除も含む）

```csharp
FolderSyncModule.Sync(
    "C:/Source",
    "C:/Target",
    mode: SyncMode.OneWay,
    syncType: SyncType.OneTime,
    scope: SyncScope.WithDeletion
);
```

### 7.3 リアルタイム監視

```csharp
FolderSyncModule.Sync(
    "C:/Source",
    "C:/Target",
    mode: SyncMode.OneWay,
    syncType: SyncType.Realtime,
    scope: SyncScope.DiffOnly
);
// ファイル変更を自動同期...
// 終了時:
FolderSyncModule.StopRealtimeSync();
```

### 7.4 双方向同期

```csharp
FolderSyncModule.Sync(
    "C:/FolderA",
    "C:/FolderB",
    mode: SyncMode.TwoWay,
    syncType: SyncType.OneTime,
    scope: SyncScope.DiffOnly
);
// A と B が同期される
```

---

## 8. エラーハンドリング

| 例外 | 発生箇所 | 対応 |
|------|---------|------|
| **DirectoryNotFoundException** | SyncCoordinator.ValidatePaths() | ソースディレクトリが存在しない |
| **UnauthorizedAccessException** | FileSystemOperations | ファイル操作権限なし |
| **IOException** | File.Copy/Delete | ファイルロック状態 |

---

## 9. パフォーマンス考慮

### 9.1 最適化ポイント

| 項目 | 最適化方法 |
|------|----------|
| **大容量ファイル** | DiffOnly で差分のみコピー |
| **小ファイル多数** | バッチ処理（将来実装） |
| **リアルタイム** | 500ms の待機で重複呼び出し防止 |
| **ディレクトリ監視** | IncludeSubdirectories で再帰監視 |

### 9.2 スケーラビリティ

- 単向同期: 制限なし
- 双方向同期: 同期ターゲット数に依存
- リアルタイム監視: ファイルシステムウォッチャーの制限に従う

---

## 10. テスト戦略

### 10.1 テスト範囲

| クラス | ユニットテスト | 統合テスト |
|--------|----------------|----------|
| FileSystemOperations | ✓ 5個 | - |
| DiffDetector | ✓ 5個 | - |
| SyncStrategy | ✓ 6個 | - |
| SyncCoordinator | - | ✓ 4個 |
| **合計** | **16個** | **4個** |

### 10.2 テスト方針

- **ユニットテスト**: 各クラスの責務を個別検証
- **統合テスト**: 複数クラスの相互作用を検証
- **テンポラリディレクトリ**: テスト実行ごとにクリーンアップ

---

## 11. 拡張性・保守性

### 11.1 新しい同期戦略の追加

```csharp
// 新しい戦略クラスを作成
public class CustomSyncStrategy : ISyncStrategy
{
    public void SyncFiles(string sourcePath, string targetPath)
    {
        // カスタムロジック
    }
    
    public void DeleteOrphanedFiles(string sourcePath, string targetPath)
    {
        // カスタムロジック
    }
}

// SyncCoordinator.CreateStrategy() に追加
private ISyncStrategy CreateStrategy(SyncScope scope) => scope switch
{
    // ...
    SyncScope.Custom => new CustomSyncStrategy(),
    // ...
};
```

### 11.2 新しい監視タイプの追加

- `RealtimeSyncWatcher` を継承
- カスタム検知ロジックを実装
- `SyncCoordinator` に統合

---

## 12. 今後の改善案

| 機能 | 優先度 | 説明 |
|------|--------|------|
| ファイルフィルタ | 中 | `.gitignore` 形式の除外ルール |
| 圧縮転送 | 低 | ネットワーク転送時の圧縮 |
| 復旧ポイント | 中 | 中断時の再開機能 |
| ロギング/監視 | 中 | 詳細なログ出力 |
| バッチ処理 | 高 | 大量ファイル時の最適化 |
| プラグイン | 低 | カスタム処理のプラグイン化 |

---

## 13. 技術スタック

- **言語**: C# (.NET 10.0)
- **テストフレームワーク**: xUnit
- **デザインパターン**: Strategy, Facade, Observer
- **キーAPI**:
  - `System.IO.FileSystemWatcher` (リアルタイム監視)
  - `System.IO.File` (ファイル操作)
  - `System.IO.Directory` (ディレクトリ操作)

---

## 14. ライセンス・著作権

- 作成日: 2026-05-17
- バージョン: 1.0
- ステータス: 本番化完了
