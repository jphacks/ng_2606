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

`.env` をメモ帳で開き、`ANTHROPIC_API_KEY=` のあとに API キーを貼って保存します。`.env` は Git に入りません。

| 設定 | 意味 |
| --- | --- |
| `ANTHROPIC_API_KEY` | Anthropic の API キー。空なら「試しのモード」（Claude を呼ばず、決まった答えを返す） |
| `APP_TOKEN` | アプリとの合言葉。アプリはヘッダー `X-App-Token` で送る。空なら合言葉なしで通す（手元の試しだけ） |
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

## 置き場所

インターネットに置く場所は、まだ決まっていません（00_overview.md 8章）。決まるまでは、開発する人の PC で動かします。
