# 共通コード（Shared）

4人の部品は、このフォルダのクラスだけを通してやり取りします。相手を直接呼びません。

**仕様の本体は [ClaudeCode/00_overview.md](ClaudeCode/00_overview.md) の4章です。** ここはその置き場所と、実装で決めたことだけを書きます。

場所：`Assets/_Project/Shared/`　名前空間：`DeskStage.Shared`

## ファイル

| ファイル | 中身 | 仕様 |
| --- | --- | --- |
| `Layers.cs` | レイヤー番号の定数と `TerrainMask` | 4.1 |
| `StageData.cs` | `StageData`、`BoxData`、`DeskData`、`SpawnData`、`GoalData`、`Vec3` | 4.2 |
| `StageContext.cs` | 共通の掲示板。`ScanStatus`、`ScanReason`、`AppMode`、`ThermalState` | 4.3 |
| `ScanContext.cs` | スキャン中の受け渡し。`SurfaceData`、`BoxChange` | 4.4 |
| `GameEvents.cs` | C が出す知らせ。`PlacementResult` | 4.5 |
| `UiRequests.cs` | D が出す依頼。`CourseItem` | 4.6 |
| `Tests/` | EditMode テスト | — |

## 使い方

**知らせ（イベント）を受ける側**は、`OnEnable` で購読して `OnDisable` で解除します。

```csharp
void OnEnable()  => StageContext.ScanStatusChanged += OnScanStatusChanged;
void OnDisable() => StageContext.ScanStatusChanged -= OnScanStatusChanged;
```

**知らせを出す側**は `Raise〇〇()` を呼びます。

```csharp
StageContext.RaiseDeskDecided();
```

**依頼（UiRequests）**は、D が `Request〇〇()` を呼び、受ける側（A・B・C）がイベントを購読します。

```csharp
// D が出す
UiRequests.RequestSetDesk(screenPosition);

// A が受ける
UiRequests.SetDesk += OnSetDesk;
```

**レイヤー**は番号を直接書かず、定数を使います。

```csharp
Physics.Raycast(ray, out var hit, 10f, Layers.TerrainMask);
```

## 実装で決めたこと

仕様の表に書かれていない細かい点です。

- **スキャン状態は `SetScanStatus()` から変える。** 「同じ状態なら知らせを出さない」（4.3）という決まりを守るため、`ScanStatus` と `ScanReason` は読み取り専用にして、変化したときだけ `ScanStatusChanged` が出るようにしています。
- **座標は長さ3の `float` 配列。** `JsonUtility` が `Vector3` を直接扱えないためです（4.2）。計算に使うときは `Vec3.From()` / `Vec3.To()` で変換します。`Vec3.To()` は長さが3でなければ `Vector3.zero` を返すので、壊れた stage.json でも落ちません。
- **`ScanContext.Clear()`** を足しました。もういちどスキャンで面と箱を消す処理が A と B の両方に出てくるので、1か所にまとめています。
- **写真の方式（00_overview.md 9章、2026-10-24）で足した物。**
  - `ScanContext.CameraPreview`（Texture）：A がカメラの映像を入れ、D の撮影画面が映す。カメラを閉じたら null
  - `ScanContext.ScanResultReceived` / `RaiseScanResultReceived(string json)`：Claude の答えの JSON（形は Docs/LlmScanFormat.md）を A から B へ渡す。中身の確かめは B が行う
  - `UiRequests.TakePhoto` / `RequestTakePhoto()`：D の「とる」ボタンで A が1枚撮る。1枚目は真上、2枚目は斜め
  - `Surfaces`、`SurfacesChanged`、`SetDesk` は写真の方式では使いません。消すかどうかは合流（M3）の前に決めます
- **asmdef を置いています。** EditMode テストを動かすのに必要です。`autoReferenced` が有効なので、A〜D のコードからは今までどおり参照できます。

## テスト

`Assets/_Project/Shared/Tests/`。Unity の Window > General > Test Runner の EditMode から実行できます。

| テスト | 見ていること |
| --- | --- |
| `JSONにして戻すと同じ値になる` | `StageData` を `JsonUtility` で往復しても壊れない |
| `既定のStageDataはversion2である` | version が 2 |
| `Vector3と配列を往復しても同じ値になる` | `Vec3` の変換 |
| `長さが3でない配列はVector3_zeroになる` | 壊れた入力で落ちない |
| `Zeroはすべて0の長さ3の配列を返す` | `Vec3.Zero()` |
| `レイヤー番号が手順書と一致する` | 8 / 9 / 10 / 11 が 00_overview.md 3.2 の表どおり |
| `TerrainMaskはTerrainだけを指す` | マスク値が Terrain だけを拾う |
| `答えのJSONがそのまま届く` など3つ | 写真の方式の `ScanResultReceived` と `TakePhoto` |

## 変えるとき

**共通コードを変えるときは、必ず先に人に聞いてください**（`CLAUDE.md`）。4人全員が依存しているので、ここが変わると全員に影響します。
