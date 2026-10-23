# 机の上ステージ（Claude Code 向けの入口）

このリポジトリは、子どもが机の上に本物の物（段ボール箱、積み木、本）を並べ、iPhone でスキャンすると、その形が3Dのステージになって遊べるアプリです。Unity 6 と AR Foundation（ARKit）で作ります。

## 作業を始める前に必ず読むもの

1. Docs/ClaudeCode/00_overview.md（全体のルール、共通コード、座標、全員の Step）
2. 自分の担当の手順書

| 担当 | 手順書 | 作業してよいフォルダ |
| --- | --- | --- |
| A：AR・スキャン | Docs/ClaudeCode/A_ar.md | Assets/_Project/AR/ |
| B：箱・データ | Docs/ClaudeCode/B_scan_data.md | Assets/_Project/Stage/、Assets/StreamingAssets/Stages/ |
| C：ゲーム | Docs/ClaudeCode/C_gameplay.md | Assets/_Project/Gameplay/ |
| D：UI・見た目 | Docs/ClaudeCode/D_ui.md | Assets/_Project/UI/、Assets/_Project/Rendering/ |

担当が分からないときは、作業を始める前に人に聞いてください。

Docs/ClaudeCode/MANUAL.md は人向けの進め方の説明です。進み具合や「待っている物はできたか」を聞かれたときは、各手順書の「進み具合」と、MANUAL.md の8章「受け渡し表」を使って答えてください。

## 進め方（要点。詳しくは 00_overview.md の2章）

- 手順書の「進み具合」で、最初の ☐ の Step を1つだけ進める。終わったら ☑ にして、人に結果と確認方法を伝えて止まる
- 【CC】は Claude Code が行う。【人】は人に頼んで待つ。【CC＋人】は両方
- 「確認」が済むまで、その Step は終わりにしない
- .unity、.prefab、.meta を直接書き換えない。シーンとプレハブはメニュー「DeskStage/<担当>/…」のエディタ用スクリプトで作る
- 自分の担当のフォルダ以外を変えない。Assets/_Project/Shared/ の共通コードを変えるときは、必ず先に人に聞く
- 仕様にないこと、【未決】の項目、新しいパッケージ、同じ失敗が2回続いたときは、止まって人に聞く
- main に直接コミットしない。担当の頭文字のブランチ（例：a/plane-watcher）で作業する
