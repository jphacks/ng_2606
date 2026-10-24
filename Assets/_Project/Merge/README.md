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
