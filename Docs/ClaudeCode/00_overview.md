# 机の上ステージ：Claude Code 作業手順（全体）

このファイルは、Claude Code が最初に読む「全体の手順書」です。各担当の手順書（A〜D）は、このファイルの決まりを前提にしています。Claude Code は、このファイルを最後まで読んでから、担当の手順書に進んでください。

## 0. このプロジェクトについて

- 子どもが自分の机の上に、段ボール箱・積み木・本などを並べてコースを作る。それを iPhone でスキャンして、3D のアクションゲームのステージにして遊ぶアプリ。
- ステージを作るのは現実の世界の子ども。アプリの役目は、子どもが並べた物を正しく、すばやく読み取ること。画面の上で箱を足す・直す機能は、読み取りの失敗を補う「補助」だけ。
- 読み取りの方式（2026-10-24 に変更。詳しくは 9章。9章と食い違う所は 9章が優先）：iPhone で机の写真を撮り、サーバー経由で Claude（LLM）に渡して、並べた物（ブロック）ごとの位置・大きさ・高さを JSON で答えてもらう。物ごとに「その上面から机の天面まで」の箱を作る。当たり判定は箱（Box Collider）。
- 前の方式（ARKit の平らな面の検出）は使わない。4.4 の Surfaces と SurfacesChanged、4.6 の SetDesk、3.2 の ScanView レイヤーは使わなくなる。
- 遊ぶ画面：保存したステージを、画面の中の3Dステージとして遊ぶ。本物の机に重ねるARは行わない。
- 大事にすること：子どもが楽しめること、知育、現実の遊びとつながること。

## 1. 担当と手順書

| 担当 | 手順書 | 中身 |
| --- | --- | --- |
| 全員 | 00_overview.md（このファイル） | 決まり、共通コードの仕様、プロジェクトの土台 |
| A：撮影・サーバー | A_ar.md | カメラと撮影、写真の確認、サーバーとの送受信、サーバー、ビルド（9章） |
| B：スキャン・データ | B_scan_data.md | Claude の答えから箱を作る処理、読み取りの補助、stage.json の保存と読み込み |

2026-10-24 から、A と B は同じ人が担当します。A と B の間の受け渡しも、共通コードを通す決まり（2.5）はそのままにします（あとで担当を分け直せるように）。
| C：キャラ・ゲームプレイ | C_gameplay.md | キャラの移動、視点のカメラ、ゲームの進行、スタートとゴール、箱の高さの決まり |
| D：UI・見た目 | D_ui.md | 全画面、ならべかたのコツ、スキャン画面、たしかめ画面、見た目、子どもテスト |

各担当の人は、自分のPCで Claude Code に「Docs/ClaudeCode/00_overview.md と、自分の担当の手順書を読んで、次の Step を進めて」と頼みます。

## 2. Claude Code の進め方の決まり（必ず守る）

### 2.1 1つの Step ずつ進める

1. 担当の手順書の「進み具合」を見て、まだ終わっていない最初の Step を選ぶ
2. その Step の「目的」「作るもの」「やること」「確認」を全部読む
3. Step の印を見る
  - 【CC】：Claude Code が進める（コードやファイルを作る）
  - 【人】：人だけができる作業（Unity エディタの操作、Xcode、実機、物を測る、子どもテストなど）。Claude Code は作業せず、人にやり方を短く説明して、終わったら教えてもらう
  - 【CC＋人】：Claude Code がコードを作り、人がエディタや実機で確かめる
4. 「確認」の条件をすべて満たしたら、その Step の進み具合のチェック欄を ☑ にする
5. 変更をコミットし、何をしたか、人が確かめることは何かを、短く報告する
6. 次の Step には、人の「次へ」の合図があってから進む

### 2.2 止まって人に聞くとき

- 手順書に書いていないこと、手順書どうしで食い違うことに気づいたとき
- 共通コード（Assets/_Project/Shared/）を変える必要があるとき（変える案を書いて、人に確認してもらう。全員の合意が必要）
- 【未決】と書かれた項目に関わるとき（手順書に書いた「仮の決め方」で進めてよいかを聞く）
- 新しいパッケージを入れたいとき
- 確認が通らず、2回直しても通らないとき

### 2.3 やってはいけないこと

- 自分の担当フォルダと自分のテスト用シーン以外のファイルを変えない（共通コードは 2.2 のとおり）
- シーン（.unity）やプレハブ（.prefab）のファイルを、テキストとして直接書き換えない。シーンやプレハブは、エディタ用スクリプト（2.5）を作って、人に Unity のメニューから実行してもらう
- .meta ファイルを手で作らない・消さない（Unity が作る）
- main ブランチに直接コミットしない

### 2.4 Git の決まり

- 作業は「担当の英字/内容」のブランチで行う（例：a/plane-watcher、b/box-builder）
- こまめにコミットする。コミットの説明は日本語で、何をしたかを1行で
- Step が終わり、人の確認が済んだら、人がプルリクエストを作る。説明には「何を作ったか・どう確かめたか・画面の写真か動画」を書く
- 担当以外の1人が承認したら main に合流する

### 2.5 Unity での作り方の決まり

- スクリプトは Assets/_Project/<担当フォルダ>/ に置く。名前空間は DeskStage.<担当> にする（下の表）
- シーンやプレハブを作る・組み立てるときは、エディタ用スクリプトを Assets/_Project/<担当フォルダ>/Editor/ に作り、Unity のメニュー「DeskStage/<担当>/…」から実行できるようにする。人がそれを実行する
- 計算の部分（面の整え方、箱の作り方、高さの決まり、JSON の確認など）は、MonoBehaviour から切り離した普通のクラスにする。EditMode のテストを Assets/_Project/<担当フォルダ>/Tests/ に作り、エディタの Test Runner で確かめられるようにする
- 調整する数値（仮の値）は ScriptableObject の設定アセットにまとめ、Inspector で変えられるようにする。設定アセットの初期値は手順書の値にする
- 長さはメートル、角度は度。Unity の1単位＝1m
- コメントは日本語で、何のための処理かを短く書く
- 他の担当のクラスを直接呼ばない。やり取りは共通コード（4章）だけで行う
- static のイベントに登録したら、OnDisable で必ず解除する

| フォルダ | 名前空間 | 担当 |
| --- | --- | --- |
| Assets/_Project/Shared/ | DeskStage.Shared | 全員（Aが作る、変更は全員で合意） |
| Assets/_Project/AR/ | DeskStage.AR | A |
| Server/ | （Python。Unity の外） | A |
| Assets/_Project/Stage/ | DeskStage.Stage | B |
| Assets/_Project/Gameplay/ | DeskStage.Gameplay | C |
| Assets/_Project/UI/ | DeskStage.UI | D |
| Assets/_Project/Rendering/ | （素材とシェーダー） | D |
| Assets/_Project/Scenes/ | ― | Main.unity は全員、Test_A〜Test_D.unity は各担当 |
| Assets/StreamingAssets/Stages/ | ― | B |
| Docs/ | ― | 全員（自分の担当のファイル） |

### 2.6 PC で試すときの決まり

- AR は実機でしか本当には動かない。PC では AR Foundation の XR Simulation（PC上でARをまねる機能）を使うか、Bのお手本ステージ（sample-01）を読み込んで試す
- AR を使わない部品（箱の作り方、ゲーム、画面）は、PC で確かめられるように作る

## 3. 座標とレイヤーの決まり

### 3.1 ステージ座標

| 項目 | 決まり |
| --- | --- |
| 原点 | 机の天面の長方形の中心。高さ0＝机の天面 |
| 上（Y） | 真上 |
| 右（X）と前（Z） | 机の天面の長方形の辺の向き。長い方の辺を左右（X）にする |
| StageRoot | ステージ座標の原点に置く空のオブジェクト。箱、キャラ、コースの物はすべてこの子に置く |

スキャン中とたしかめ画面では、Aが本物の机の上に StageRoot を置きます。遊ぶときは、StageRoot を3D画面の原点に置きます。

### 3.2 レイヤー

| 番号 | 名前 | 使う物 |
| --- | --- | --- |
| 8 | Terrain | 机の板と箱の当たり判定 |
| 9 | ScanView | スキャン中とたしかめ画面だけ見せる物 |
| 10 | Player | キャラ |
| 11 | Gimmick | 予約（色ギミックの拡張で使う。今は使わない） |

### 3.3 箱の高さの決まり（Cが決め、全員が使う）

| 隣の面との高さの差 | 扱い |
| --- | --- |
| 0.8cm未満 | 歩いて登れる段差 |
| 0.8〜4.5cm | ジャンプで登れる段（緑の印） |
| 4.5cmを超える | 登れない壁（赤の印） |

## 4. 共通コード（Shared）の仕様

4人の部品は、この章のクラスだけを通してやり取りします。Aが全体の Step 3 でこの仕様どおりに作ります。名前は下の表のとおりにそろえます（各担当の手順書もこの名前を使います）。

### 4.1 Layers（レイヤー番号の定数）

Terrain＝8、ScanView＝9、Player＝10、Gimmick＝11 を定数として持ち、Terrain だけを対象にするマスク値も持ちます。各担当はレイヤー番号を直接書かず、この定数を使います。

### 4.2 StageData（stage.json と同じ形のデータ）

Unity 標準の JsonUtility で読み書きできる形にします。座標は「長さ3の数値の配列」で持ち、Vector3 との変換用の小さな補助関数を同じファイルに置きます。

| 項目 | 型 | 内容 |
| --- | --- | --- |
| version | 整数 | 2 |
| id | 文字列 | ステージID（フォルダ名と同じ） |
| name | 文字列 | 表示名 |
| createdAt | 文字列 | スキャンした日時（ISO 8601） |
| desk | 幅と奥行き（m） | 机の天面の長方形の大きさ。中心は原点 |
| boxes | BoxData の一覧 | 下の表 |
| spawn | 位置（長さ3）と向き（yawDegrees） | スタート地点 |
| goal | 位置（長さ3）と半径（m） | ゴール |
| checkpoints | 位置と半径の一覧 | チェックポイント（最大5個） |
| fallDepth | 数値（m） | 机の天面からどれだけ下で落下とするか。初期値0.10 |

BoxData（箱1つ）：

| 項目 | 型 | 内容 |
| --- | --- | --- |
| id | 文字列 | 例：box-01 |
| center | 長さ3（m） | 箱の中心。高さは「箱の高さの半分」 |
| size | 長さ3（m） | 幅・高さ・奥行き。高さ＝上面から机の天面まで |
| yawDegrees | 数値（度） | 上から見た回転。Y軸まわりだけ |
| source | 文字列 | auto（スキャンで作った）か manual（補助で足した・直した） |
| texture | 文字列 | 表面の画像のファイル名。なければ空 |

### 4.3 StageContext（共通の掲示板）

| 持つ情報 | 書く人 | 内容 |
| --- | --- | --- |
| CurrentStage | B | 今のステージデータ |
| StageRoot | A（スキャン中）、B（遊ぶとき） | ステージ座標の原点のオブジェクト |
| ScanStatus と ScanReason | A | 下のスキャン状態と、Limited の理由 |
| Mode | D | 今の場面：Menu、Scan、Check、Play |

スキャン状態（ScanStatus）：NotStarted、Starting、FindingDesk、Scanning、Limited、Lost、Finished。Limited の理由（ScanReason）：None、Dark、FastMotion、FewFeatures。

| 知らせ（イベント） | 出す人 | 受ける人 | いつ |
| --- | --- | --- | --- |
| StageLoaded | B | 全員 | stage.json を読み終えたとき |
| ScanStatusChanged | A | B、D | スキャン状態が変わったとき（同じ状態なら出さない） |
| DeskDecided | A | B、D | 机を決め、StageRoot を置いたとき |
| StageBuilt | B | C、D | 机の板と箱のオブジェクトを作り終えたとき |
| SaveCompleted（成功か、理由） | B | D | 保存が終わったとき |
| StageListChanged | B | D | ステージ一覧（StageList、1件ごとに id・表示名・作った日時・読み込めるか・理由）が変わったとき（2026-10-24 に足した） |
| ThermalStateChanged（4段階） | A | D | 端末の発熱状態が変わったとき |

### 4.4 ScanContext（スキャン中の受け渡し）

| 持つ情報 | 書く人 | 内容 |
| --- | --- | --- |
| DeskSize | A | 机の長方形の幅と奥行き |
| Surfaces | A | 整えた上面の一覧。1件ごとに：番号（id）、高さ（m）、上から見た形（ステージ座標の XZ の点の並び）、最後に変わった時刻 |
| WorkingBoxes | B | 作業中の箱の一覧（BoxData）。保存するまではここにある |
| SelectedBoxId | B | 補助で選んでいる箱の id（なければ空） |

| 知らせ | 出す人 | 受ける人 | いつ |
| --- | --- | --- | --- |
| SurfacesChanged | A | B、D | 上面が増えた・変わった・消えたとき（1秒に4回まで） |
| BoxesChanged（箱の id、足した・変えた・消した、auto か manual か） | B | D | 箱が変わったとき |

### 4.5 GameEvents（C が出す）

| 知らせ | 一緒に渡す情報 | いつ |
| --- | --- | --- |
| GameStarted | なし | キャラがスタートに出たとき |
| CheckpointPassed | 何番目か | 初めて触れたとき |
| Goal | タイム（秒）、最速記録かどうか | ゴールに触れたとき |
| Fell | なし | 落下と判定したとき |
| ElapsedTime | 秒 | プレイ中、毎フレーム |
| CoursePlaced | 置いた物、結果（置けた、置けない、とどかないかも）、理由 | コースの物を置こうとしたとき |

### 4.6 UiRequests（D が出す依頼）

| 依頼 | 一緒に渡す情報 | 受ける人 |
| --- | --- | --- |
| SelectStage | ステージID | B |
| StartScan | なし | A |
| SetDesk | 画面のタップ位置 | A |
| FinishScan | なし | A、B |
| Rescan | なし | A、B |
| AddBox | 画面のタップ位置 | B |
| SelectBox | 画面のタップ位置 | B |
| MoveBox | 机の面に沿った移動量（m、XZ） | B |
| ResizeBox | 幅・高さ・奥行きの増減（m） | B |
| RotateBox | 角度（度） | B |
| DeleteBox | なし | B |
| UndoBox | なし | B |
| PlaceCourseItem | 置く物（Start、Goal、Checkpoint）、画面のタップ位置 | C |
| SaveStage | 表示名 | B |
| Play | なし | C |
| OrbitView | 横と縦の量 | C |
| ZoomView | 近づける量 | C |
| SetPaused | 止めるかどうか | C |
| Retry | なし | C |
| BackToList | なし | B、C |
| DeleteStage | ステージID | B（2026-10-24 に足した） |

### 4.7 入力（DeskStageControls）

Input System のアセット Assets/_Project/Shared/Input/DeskStageControls.inputactions。D が作ります。アクションマップ Player に、Move（Value / Vector2、ゲームパッドの左スティックとキーボードの WASD）と Jump（Button、ゲームパッドの下のボタンとキーボードのスペース）。C# クラスを作る設定にし、クラス名 DeskStageControls、名前空間 DeskStage.Shared にします。

### 4.8 呼び出しの順番

1. 子ども：机の上に物を並べる（D：ならべかたのコツ画面）
2. D：StartScan → A：AR を始め、FindingDesk
3. D：SetDesk → A：机を決めて StageRoot を置き、DeskDecided、Scanning
4. A：上面を整えて Surfaces に書き、SurfacesChanged
5. B：上面から箱を作り、BoxesChanged → D：箱が生える演出
6. D：FinishScan → A：検出を止めて Finished。B：箱を確定
7. D：たしかめ画面。補助の依頼 → B。並べ直したら Rescan → 2 へ
8. D：PlaceCourseItem → C：本物の物の上（箱の上面）に置き、CoursePlaced
9. D：SaveStage → B：保存して SaveCompleted。A：AR を止める
10. D：Play → C：3D画面でスタートにキャラを出し、GameStarted

保存済みのステージ：D：SelectStage → B：読み込んで StageLoaded、組み立てて StageBuilt → D：Play → C

## 5. 全体の Step（プロジェクトの土台。A が行う）

### 進み具合

- ☑ 全体 Step 1：プロジェクトの作成
- ☐ 全体 Step 2：Git と GitHub
- ☐ 全体 Step 3：共通コードを作る
- ☐ 全体 Step 4：全員が受け取る

### 全体 Step 1：プロジェクトの作成【人】

- 目的：全員が同じ Unity プロジェクトで作業できる土台を作る
- やること（人が Unity Hub とエディタで行う）
  1. Unity Hub で Unity 6 LTS を入れる。モジュールで iOS Build Support を選ぶ
  2. Universal 3D テンプレートで、名前 DeskStageAR のプロジェクトを作る
  3. Package Manager で AR Foundation、Apple ARKit XR Plugin、Input System を入れる。新しい入力方式を有効にするか聞かれたら Yes
  4. Build Profiles で iOS に切り替える
  5. XR Plug-in Management の iOS で Apple ARKit にチェック、Requirement を Required
  6. Player の iOS：Camera Usage Description に「机の上をゲームのステージにするためにカメラを使います」、Bundle Identifier に他人と重ならない名前、Architecture は ARM64
  7. iOS で使う Universal Renderer に AR Background Renderer Feature を追加する（ないとカメラ映像が真っ黒になる）
  8. Tags and Layers で 8 Terrain、9 ScanView、10 Player、11 Gimmick を作る
  9. Editor の設定で Asset Serialization を Force Text にする
- 確認：エディタがエラーなく開き、上の設定がすべて入っている

### 全体 Step 2：Git と GitHub【CC＋人】

- 目的：4人が安全に同じプロジェクトを変えられるようにする
- やること
  1. 【CC】プロジェクトの一番上に、Unity 用の .gitignore（GitHub の Unity.gitignore と同じ内容）と、Git LFS の設定ファイル .gitattributes（.png、.jpg、.psd、.wav、.ttf、.otf を LFS で管理）を作る
  2. 【CC】フォルダ Assets/_Project/ の下に Shared、AR、Stage、Gameplay、Rendering、UI、Scenes と、Docs/ を作る（空のフォルダには説明の README.md を1つ置く）
  3. 【CC】プロジェクトの一番上に CLAUDE.md を置く（この手順書のフォルダを最初に読むよう書いた短いファイル。配布されたものを使う）
  4. 【人】git lfs install、最初のコミット、GitHub の非公開リポジトリに上げる、3人を招待、main を保護（プルリクエストと承認1件が必要）
- 確認：GitHub に main があり、3人が招待されている

### 全体 Step 3：共通コードを作る【CC】

- 目的：4章の仕様どおりの共通コードを作り、全員が使えるようにする
- 作るもの（Assets/_Project/Shared/）
  - Layers、StageData（BoxData などを含む）、StageContext、ScanContext、GameEvents、UiRequests
  - 名前空間はすべて DeskStage.Shared
- やること
  1. 4章の表のとおりの名前と情報でクラスを作る。知らせは static のイベント、依頼は static の「出す関数」と「受けるイベント」の組にする
  2. StageData に、Vector3 と配列の変換の補助関数を置く
  3. EditMode テスト（Assets/_Project/Shared/Tests/）：StageData を JSON にして戻すと同じ値になること、Vector3 の変換が合っていること
  4. 共通コードの中身の説明を Docs/SharedCode.md に書く（4章へのリンクでよい）
- 確認：コンパイルエラーがない。Test Runner の EditMode テストがすべて通る。全員が中身を見て了承した（人）

### 全体 Step 4：全員が受け取る【人】

- やること：全員が git lfs install のあと git clone し、Unity Hub で開く
- 確認：全員のPCでエラーなく開け、Assets/_Project/ が見える

## 6. フェーズと合流の目安

| フェーズ | ゴール | 全員で確かめること |
| --- | --- | --- |
| P0 | 土台と、平らな面の検出の実機検証 | 判定会議で案A〜Dを決める（A_ar.md の Step） |
| M1 | 並べた物をスキャンすると箱が生える | 実機でお手本の並べ方をスキャンして箱が生える |
| M2 | 箱づくりの完成、もういちどスキャン、補助、ならべかたのコツ、保存と読み込み | 子どもテスト1回目で、子どもが並べてスキャンし遊べる |
| M3 | 合流：並べる→スキャン→たしかめる→保存→遊ぶ→並べ直す | スキャンしたステージを保存して遊べる |
| M4 | ルール、エラー処理、照明のテスト | 子どもテスト2回目、時間の計測 |
| M5 | 見た目、文言、音、完成ビルド | 品質目標の最終確認 |

### 合流（M3）の決まり

- 各担当は自分の部品をプレハブにまとめる（AR_System、Stage_System、Game_System、UI_System）。Main.unity にはそのプレハブを置くだけにする
- Main.unity を変える人は、チャットで「Main触ります」と言い、終わったら「Main終わりました」と言う
- 合流した main は、Mac を持つ人（A）が週1回以上ビルドして全員の iPhone に入れる（無料の Apple ID のアプリは7日で開けなくなる）

## 7. 品質目標（仮の値。P0 と M2 の結果で見直す）

| 項目 | 目標 |
| --- | --- |
| 読み取りの正しさ | 補助なしで、並べた物の8割以上で箱が合う |
| キャラが乗る面の高さ | 実物との差±1cm以内 |
| 並べ終わってから遊べるまで | 3分以内 |
| もういちどスキャン | 60秒以内に新しい並べ方がそろう |
| 説明なしで並べて遊べる | 子ども（子ども役）3人中2人以上 |
| フレームレート | 遊ぶ画面60fps、スキャン中30fps以上 |

## 8. 未決事項（Claude Code はここに関わるとき、人に確認する）

| 未決事項 | 決まるまでの仮の決め方 |
| --- | --- |
| 遊ぶ画面の背景（カメラ映像か、完全な3Dか） | 完全な3D画面として作る |
| P0 の判定でどの案（A〜D）にするか | 案A（家にある物を自由に並べる）として作る |
| 対象の年齢 | 小学校低学年として、画面の文字はひらがな中心 |
| 色ギミックの拡張 | 作らない。レイヤー11番の予約だけ |
| version 1 のステージの変換 | 変換しない。「読み込めません（古い形式）」と表示 |
| サーバーを置く場所 | 無料枠のある小さなサーバー（Cloud Run など）。決まるまでは開発者の PC で動かす |
| サーバーを作る担当 | A（9.6）。答えの形と指示文は B |
| 写真の枚数と撮り方 | 真上から1枚と、手前から斜めに1枚の、2枚 |
| 長さの基準 | A4 のコピー用紙（21.0cm × 29.7cm）を机の左手前の角に置くと正確になる。紙がなくても、身の回りの物の大きさから推定して箱を作る（2026-10-24 に決めた） |
| 読み取りの正しさの目標（7章） | 物の数は8割以上。高さは±1cm を目指すが、届かない所はたしかめ画面の補助で直す。最初の試しの結果で見直す |
| API の料金の上限 | 開発中は1日あたりの回数をサーバーで制限する。上限の値はチームで決める |

## 9. 写真と LLM による読み取り（2026-10-24 に決めた変更）

この章は、チームの判断で読み取りの方式を変えたときの決まりです。この章と、ほかの章や各担当の手順書が食い違うときは、この章を優先します。各担当の手順書は、それぞれの担当が自分で書き直します。

### 9.1 決めたこと

| 項目 | 決め方 |
| --- | --- |
| 箱の見分け | 写真を Claude に渡し、物（ブロック）ごとに答えてもらう |
| 長さ・高さ | 写真だけから Claude が推定する。ARKit は使わない。A4 の紙を一緒に写すと正確になるが、紙がなくても箱を作る |
| 使う LLM | Claude（モデル ID は `claude-opus-5-5`） |
| API キー | サーバーにだけ置く。iPhone のアプリには入れない |
| 遊ぶ画面 | 今までどおり3D画面。たしかめ画面も、カメラ映像ではなく3D画面に箱を出す |

### 9.2 呼び出しの順番（4.8 の置き換え）

1. 子ども：机の上に物を並べる。A4 の紙があれば机の左手前の角に置く（なくてもよい。D：ならべかたのコツ画面で絵で伝える）
2. D：StartScan → A：カメラを開く（AR は使わない。Unity 標準の WebCamTexture）。状態は FindingDesk（1枚目を撮る）
3. D：TakePhoto → A：真上の写真を撮る。状態は Scanning
4. D：TakePhoto → A：斜めの写真を撮り、2枚をサーバーへ送る（UnityWebRequest）
5. サーバー：2枚の写真を Claude に渡し、決まった形の JSON を受け取って、形を確かめてから A に返す
6. A：答えの JSON を ScanContext の ScanResultReceived で B に渡す。状態は Finished
7. B：答えから箱を作り（9.4）、整え（Step 7 の BoxRefiner）、WorkingBoxes に書いて BoxesChanged → D：箱が生える演出
8. D：たしかめ画面（3D）。補助の依頼 → B。並べ直したら Rescan → 2 へ
9. 以降（コースの物を置く、保存、遊ぶ）は 4.8 の 8〜10 と同じ

うまくいかないとき：写真が暗い・ぶれている（A が送る前に調べる）は Limited と理由（Dark、FastMotion）。通信の失敗や、サーバーの答えの形が違うときは Lost。D は「もういちど とってね」と出す。

### 9.3 共通コードの変更（全員の合意が要る。2.2 のとおり）

| 変更 | 内容 |
| --- | --- |
| ScanContext に足す | 知らせ `ScanResultReceived`（A → B、答えの JSON の文字列）と `RaiseScanResultReceived(string json)` |
| UiRequests に足す | 依頼 `TakePhoto`（なし、D → A） |
| ScanContext に足す | `CameraPreview`（Texture。A が書き、D の撮影画面が映す） |
| 使わなくなる物 | `ScanContext.Surfaces`、`SurfacesChanged`、`UiRequests.SetDesk`、`DeskDecided`、`Layers.ScanView`。すぐには消さず、合流（M3）の前に消すか決める |
| そのまま使う物 | StageData、StageContext（StageRoot は B が原点に作る）、ScanStatus（意味は 9.2 のとおり読み替える）、WorkingBoxes、BoxesChanged、FinishScan、Rescan など |

### 9.4 サーバーの答えの形（B が決め、A のサーバーと B が使う）

答えは Claude の構造化出力（structured outputs）で、次の形に固定します。くわしい形と指示文は B が Docs/LlmScanFormat.md に書きます。

| 項目 | 内容 |
| --- | --- |
| paperFound | 基準の紙が写っていたか（true / false）。false でも箱は作る。D は「かみを おくと もっと ぴったりになるよ」と出してよい |
| scaleReference | 長さの基準に使った物（例：A4 の紙、マーカーの箱、机の天板の木目） |
| desk | 机の天面の幅と奥行き（cm）。机の端が写っていなければ null |
| blocks | 物の一覧。1件ごとに：label（何の物か）、中心の位置（cm）、幅・奥行き・高さ（cm）、上から見た回転（度）、自信の度合い（0〜1） |

位置の基準は、紙があれば紙の左手前の角、紙がなければ物を全部囲む長方形の左手前の角を原点にし、右を +X、奥を +Z、上を +Y にします。B がステージ座標（3.1、m 単位、机の中心が原点）に直します。机が写っていないときは、物を囲む長方形に 10cm の余白を付けた範囲を机にします。

### 9.5 写真と子どもの情報の扱い

- 写真はサーバーに保存しない。ログにも残さない。Claude に渡すだけにする
- 撮る前に、画面で「しゃしんを おくるよ」と知らせる（D）
- 子どもテストの前に、保護者に「写真を外部のサービスに送って解析すること」を説明する

### 9.6 担当の変わる所

| 担当 | 変わる所 |
| --- | --- |
| A | B と同じ人が担当する。面の検出と机の決定（A の Step 6〜13 の多く）は使わない。代わりに、カメラと撮影、写真の明るさ・ぶれの確認、サーバーとの送受信、サーバー自体（API キーの管理、1日の回数の制限、答えの形の確認）を作る |
| B | 上面から箱を作る（BoxBuilder）代わりに、答えの JSON から箱を作る。答えの形と指示文も B が決める。整える・補助・保存・読み込みは今までどおり |
| C | ほぼ変わらない。たしかめ画面が3Dになるので、コースの物を置くタップは3D画面の箱の上で受ける |
| D | スキャン画面を「撮影画面（紙の置き方とガイド枠、2枚の案内）」と「待ち画面」にする。たしかめ画面を3Dにする |
