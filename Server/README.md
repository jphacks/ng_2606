# 読み取りサーバー（A の Step 24）

机の写真2枚を受け取り、Claude（`claude-opus-5-5`）に渡して、物ごとの位置と大きさを JSON で返します。答えの形は B のスキーマ（`Assets/_Project/Stage/Llm/scan_result.schema.json`）、指示文は `Assets/_Project/Stage/Llm/scan_prompt.md` を、起動のときに読み込みます。

- API キーはこのサーバーにだけ置きます。アプリにもリポジトリにも書きません
- 写真は保存しません。ログにも書きません。ログに残すのは、使ったトークンの量だけです（料金の目安）
- Claude が答えを断ったときは、Anthropic がすすめる別のモデルで答え直します（`fallbacks: "default"`）

## はじめての準備（Windows）

```
cd Server
python -m venv .venv
.venv\Scripts\python -m pip install -r requirements.txt
copy .env.example .env
```

`.env` をメモ帳で開き、`ANTHROPIC_API_KEY=` のあとに API キーを、`APP_TOKEN=` のあとに自分で決めた合言葉（英数字で長めに）を入れて保存します。`.env` は Git に入りません。

| 設定 | 意味 |
| --- | --- |
| `ANTHROPIC_API_KEY` | Anthropic の API キー。空なら「試しのモード」（Claude を呼ばず、決まった答えを返す） |
| `APP_TOKEN` | アプリとの合言葉。アプリはヘッダー `X-App-Token` で送る。API キーを入れたときは必ず決める（空だとサーバーが起動しない）。試しのモードでは空でもよい |
| `DAILY_LIMIT` | 1日の読み取りの上限の回数（初期値 100）。サーバーを止めると0に戻る |
| `MOCK` | 1 にすると、キーがあっても試しのモードになる |

## 動かす

```
cd Server
.venv\Scripts\python -m uvicorn app:app --host 0.0.0.0 --port 8000
```

- `http://localhost:8000/health` を開くと、`{"ok": true, "mock": false, ...}` のように出ます。`mock` が true なら試しのモードです
- iPhone から同じ Wi-Fi で試すときは、PC の IP アドレス（例：`http://192.168.0.10:8000`）を使います。Windows のファイアウォールで許可を聞かれたら、プライベートネットワークを許可します

## 読み取りを試す

```
curl -H "X-App-Token: 合言葉" -F "top=@top.jpg;type=image/jpeg" -F "side=@side.jpg;type=image/jpeg" http://localhost:8000/scan
```

`top` は真上、`side` は手前から斜めの写真です。1回で約 $0.07〜0.10 かかります（試しのモードは無料）。

| 返る番号 | 意味 |
| --- | --- |
| 200 | 答えの JSON |
| 401 | 合言葉が違う |
| 413 | 写真が 5MB より大きい |
| 422 | 写真が2枚そろっていない |
| 429 | 今日の上限を超えた |
| 502 | Claude の API のエラー、答えを断られた、答えの形が違う |
| 503 | 混んでいる（少し待って撮り直す） |

## みんなの ステージ（投稿）

遊ぶ人が作ったステージを、このサーバーに投稿して、同じ Wi-Fi の人どうしで遊べます（Docs/specs/home-modes）。どの口も、ヘッダー `X-App-Token` の合言葉が要ります。写真は受け取りません。ログに残すのは、投稿の id と件数だけです。

| 口 | 意味 |
| --- | --- |
| `POST /stages` | ステージの JSON（StageData の形）を投稿する。確かめてから、決めた項目だけを組み立て直して保存し、`{"id": "c-…"}` を返す。id と投稿日時はサーバーが決める。箱の `texture`（写真のファイル名）は空にする |
| `GET /stages` | 投稿の一覧（新しい順、100 件まで）。`{"stages": [{"id", "name", "createdAt", "boxCount", "hasRules"}]}` |
| `GET /stages/{id}` | 1 件の JSON。id は `c-` ＋英数字とハイフンだけ |

- 保存先は `Server/shared_stages/<id>.json`（`SHARED_STAGES_DIR` で変えられる）。Git には入りません。不適切な投稿は、このフォルダからファイルを消します
- 確かめること：大きさ 200KB まで、箱 100 個まで、チェックポイント 5 個まで、名前 30 文字まで（長い分は切る。制御文字は除く。空なら「ステージ」）、数値は有限で範囲の中（位置 -2〜2 m、箱の大きさ 0 より大きく 1 m 以下、半径 0〜0.5 m、fallDepth 0〜1 m、maxJumps -1〜99、制限時間 0〜600 秒、flow 0〜4）、しかけは `water`・`fire`・`heal` か空
- 投稿は 500 件までです。超えると断ります

| 返る番号 | 意味 |
| --- | --- |
| 200 | 投稿できた・読めた |
| 400 | 形が違う（JSON でない、数値が範囲の外、箱が多すぎる、id の形が違う など） |
| 401 | 合言葉が違う |
| 404 | その id のステージは無い |
| 413 | 200KB より大きい |
| 507 | 投稿の数が上限（500 件）に達した |

## 合言葉について

アプリに入れた合言葉は、アプリを調べれば取り出せます。強い秘密ではなく、いたずらで呼ばれるのを減らすためのものです。

- 大事な API キーは、このサーバーにだけ置きます
- 料金の使いすぎは、1日の回数の上限（`DAILY_LIMIT`）と、Anthropic のコンソールの月の使用上限で防ぎます
- インターネットに置くときは、必ず `https://` にします。アプリは、`http://` では自分の PC か、開発用ビルドで家の Wi-Fi の中のサーバーにしか送りません

## 置き場所

インターネットに置く場所は、まだ決まっていません（00_overview.md 8章）。決まるまでは、開発する人の PC で動かします。
