# デバッグ表示（DebugOverlay）の使い方

担当：D（D_ui.md Step 6）。

## 出し方

- iPhone：3本指で2回たたく（0.4秒以内）。もう一度で消える
- PC（エディタ）：F1 キー
- Development Build でないビルドでは出ない

## 最初から出るもの

FPS、スキャン状態と理由、スキャン開始からの秒数、上面の数、箱の数（auto・manual）、場面（Mode）、ゲームの状態。
表示中に箱（Terrain レイヤーの当たり判定）をタップすると、その箱の id、大きさ、高さ（cm、StageRoot からの高さ）が出る。箱は「オブジェクトの名前＝箱の id」で探す（B の StageBuilder でそう名付けてもらう）。
切り替え「当たり判定の枠」で、Terrain レイヤーの Box Collider の枠を緑の線で描く。

## 各担当が項目を足す（登録の受け口）

DebugRegistry に「名前と値」「切り替え」「スライダー」を登録すると、一覧に出る。

```csharp
DebugRegistry.AddValue("上面の更新", () => lastUpdate.ToString("0.0"));
DebugRegistry.AddToggle("PlaneProbe", () => probe.enabled, v => probe.enabled = v);       // A
DebugRegistry.AddSlider("歩く速さ", 0.05f, 0.5f, () => settings.WalkSpeed, v => settings.WalkSpeed = v); // C
// OnDisable で DebugRegistry.Remove("名前") する
```

【相談中】DebugRegistry は今 D の中（DeskStage.UI）にある。他の担当のクラスを直接呼ばない決まりのため、A・C が使うには共通コード（Shared）に移す必要がある。全員の合意が取れたら D が移す。
