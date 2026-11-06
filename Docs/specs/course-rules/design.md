# course-rules の設計

## データ（共通：Shared/StageData.cs）

StageData に `rules`（CourseRules）を 1 つ足す。JsonUtility は、無い項目を最初の値のまま残すので、古い保存データは「しばり無し」になる。

```
CourseRules
  int   maxJumps = -1          // -1 むせいげん、0 きんし、1 以上はその回数まで
  bool  noDoubleJump = false   // true なら 2 段ジャンプを使えない
  bool  oneHit = false         // true なら、ダメージ 1 回でゲームオーバー
  float timeLimitSeconds = 0   // 0 なし。1 以上はその秒数で時間切れ
```

保存の写し（Stage/StageSavePrep.cs）で、rules も写す。

## 決まりの計算（Gameplay/CourseRuleState.cs、新規。画面に触れない）

- `Reset(CourseRules)`：遊び始め・やり直しで、使ったジャンプの数を 0 に
- `CanJump(bool air)`：禁止・回数切れ・空中で 2 段禁止なら false
- `OnJumped()`：使った数を 1 足す（地面からも空中からも 1 回）
- `JumpsLeft`：残りの回数（むせいげんなら -1）
- `IsTimeUp(float elapsed)`、`SecondsLeft(float elapsed)`
- `Summary(CourseRules)`：遊ぶ画面・クリアの画面に出す短い文（子ども向けの言葉は AppText に置き、ここは種類だけ返す）

## キャラ（Gameplay/CharacterMotor.cs）

ジャンプの前に、外から「跳んでよいか」を聞く口を足す。決まりを持たない（ゲームの担当 C の部品は、しばりを知らない）。

- `Func<bool, bool> JumpAllowed`（引数は空中か。未設定なら、いつでも跳べる）
- `event Action<bool> Jumped`（跳んだあとに呼ぶ）
- 地面のジャンプは、先行入力（JumpTimer）を使い切る前に聞く。跳べないときは、入力を捨てる（あとで勝手に跳ばない）

## しばりの係（App/CourseRuleSystem.cs、新規）

AppRoot が GimmickSystem と同じ所に付ける。

- GameStarted で、いまのステージの rules を読んで CourseRuleState を Reset し、GameFlow.Motor の JumpAllowed / Jumped につなぐ
- 毎フレーム、プレイ中で時間切れなら `GameOver(理由)` を出す
- GimmickSystem.Damaged を受けて、oneHit なら `GameOver(理由)` を出す
- AppFlow は、GimmickSystem.GameOver と同じく、これを受けて、ゲームオーバーの画面に理由を渡す

## 画面（App/）

- CheckStageScreen：コースの段を、3 つのボタン（スタート・ゴール・しばり）にする。「しばり」で、しばりを選ぶ板（RulesPanel。4 行。左右のボタンで値を変える）を出す。「きめた」で閉じる。値は StageContext.CurrentStage.rules に入れる
- PlayScreen：上の帯に、付けたしばりの短い文と、残り（ジャンプ・秒）を出す
- GameOverScreen：OnEnter の引数で、理由の文を出す（無ければ、これまでの文）
- ClearScreen：そのときのしばりの短い文を出す

## テスト

- EditMode：CourseRuleState（禁止・回数・2 段・時間切れ・Reset）、古い保存データの読み込み、保存の写し
- PlayMode：JumpAllowed が false なら、ジャンプを押しても跳ばない。回数まで跳んだら、それ以上跳ばない
