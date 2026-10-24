# Merge（合流のつなぎ）

A・B・C・D の部品を、1つのアプリ（`Scenes/Main.unity`）にまとめるための、**どの担当にも属さない**つなぎです。

- `MergeFlow`：D の画面（ScreenRouter）と、A・B・C の動きの受け渡し
  - 撮影の答えが届いたら、たしかめ画面へ
  - たしかめ画面で「あそぶ」を押したら、`UiRequests.Play`（C が受ける）
  - 遊ぶ画面から出たら、`UiRequests.BackToList`
  - ゴールしたら、クリア画面へ
- `PhotoScanView`：撮影画面（カメラの映像、「とる」ボタン、案内）。D の撮影画面ができるまでの代わり
- `CoursePlaceView`：スタートとゴールを置くボタン。D の「コースの物を置く」画面ができるまでの代わり
- `Prefabs/UI_System.prefab`：`Scenes/Test_D.unity` から、開発用の仮の部品（`DEV_*`、`DevEvents`、`DevMarker`）を除いて作った UI。
  メニュー「DeskStage/Merge/1. UI_System プレハブを作る」で作り直せる

**D の担当が正式な `UI_System` と撮影画面を作ったら、この中の `UI_System.prefab`、`PhotoScanView`、`CoursePlaceView` は、差し替えて消す。**

## 写真のキャラクター（人形）

- 撮る：カメラの映像に、太めで 3 等身くらいの人型の枠（`HumanMask`）を重ねる。人形を枠に合わせて撮ると、枠の中だけがキャラになる（枠の形で切り抜く）
- `CutoutMath`（背景の色で自動で切り抜く方法）は、うまく切れなかったので、今は使っていない。テストだけ残してある。不要になったら消してよい
- 保存：端末の中（`CharacterStore`）。選んだキャラは、遊ぶとき（`CharacterAvatar`）に、キャラの体の代わりに立つ
