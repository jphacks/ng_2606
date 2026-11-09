# warp-pairs の設計

## データ（共通：Shared/StageData.cs）

- BoxData に `warpTo`（つながる相手のピンクの箱の id。無ければ空）を足す
- 写す所（StageSavePrep、BoxAssistCore.Copy、BoxRefiner.Clone）で、warpTo も写す
- サーバーの投稿の確かめ（Server/app.py）で、warpTo（箱の id と同じ形）を残す。投稿の中の箱の id を付け直したときは、warpTo も付け直す。相手がいなければ空にする

## つなぎ方の計算（App/WarpPairs.cs、新規。画面に触れない）

- `Repair(boxes)`：ピンクでない相手、いない相手、相手が自分を指していない物を、空にする（行き来の対称を保つ）
- `AutoPair(boxes)`：相手がいないピンクを、上から見た距離が近い順に、2 つずつつなぐ
- `Cycle(boxes, id)`：その箱の相手を、次のピンクの箱（距離の近い順。いまの相手の次）に変える。前の相手と、新しい相手の前の相手は空にしてから、AutoPair で余った物どうしをつなぐ
- `Partner(boxes, id)`、`Numbers(boxes)`（ピンクに 1, 2, 3… の番号。ボタンの表示用）

## 遊ぶとき（App/GimmickSystem.cs）

- ワープの箱に乗ったら、相手の箱の上面の真ん中へ Teleport。キャラが入れなければ、上面の中を少しずつずらして探す。見つからなければ飛ばない
- 相手がいなければ、何もしない
- しかけの見た目を作り直すとき（Rebuild）に、作業中の箱と、読み込んだ箱の両方に、Repair と AutoPair をかける

## たしかめの画面（App/CheckStageScreen.cs）

- 水の「ながれの むき」と同じ並びで、ピンクの箱の上に「1 ⇄ 2」のボタンを出す。押すと Cycle
- 撮ったあとのしかけのひとことに、ピンクが 1 つだけなら「ワープは ピンクが 2こ いるよ」を足す

## テスト

- EditMode：WarpPairs（自動でつなぐ・奇数・押して変える・対称・壊れた相手の修理）、BoxData の warpTo の保存と古いデータ
- サーバー：warpTo が残る、知らない id の warpTo は空になる
