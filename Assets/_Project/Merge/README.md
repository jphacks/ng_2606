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

- 撮る：カメラの映像に、太めで 3 等身くらいの人型の枠（`HumanMask`）が重なる。人形を枠に合わせて撮ると、枠の中だけがキャラになる（枠の形で切り抜く）
- 使っていない物：`CutoutMath`（背景の色で自動で切り抜く）と `BackgroundSubtraction`（背景との差で取り出す）。どちらも、人形の撮り方としては採らなかった。テストごと残してある。不要になったら消してよい
- 保存：端末の中（`CharacterStore`）。選んだキャラは、遊ぶとき（`CharacterAvatar`）に、キャラの体の代わりに立つ。見た目の大きさは、当たり判定の高さ（3cm）の 1.5 倍

## ステージの背景

- 背景専用に撮った写真を、1 枚だけ、端末の中に保存する（`BackdropStore`）。一覧の画面の「はいけいを とる」で、撮る・なくす（`BackdropPanelView`）
- ステージができると、その写真を、ぼかして、ステージの周りの大きなドームの内側に貼る（`StagePhotoTexturer`）。写真が無ければ、背景は出ない
- 撮影の 2 枚目（斜め）の写真は、背景には使わない（汚れが入るため）
