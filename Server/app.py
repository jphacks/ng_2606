"""
机の写真を Claude に渡して、物ごとの位置と大きさを JSON で返すサーバー（A の Step 24）。

- POST /scan：写真2枚（top＝真上、side＝斜め）を受け取り、答えの JSON を返す
- 答えの形は B のスキーマ（Assets/_Project/Stage/Llm/scan_result.schema.json）で固定する
- API キーはこのサーバーにだけ置く。写真は保存しないし、ログにも書かない（00_overview.md 9.5）
"""

import base64
import datetime
import hmac
import json
import logging
import os
import pathlib
import threading

import anthropic
from fastapi import FastAPI, File, Header, HTTPException, Request, UploadFile
from fastapi.responses import JSONResponse

HERE = pathlib.Path(__file__).resolve().parent


def load_env_file(path: pathlib.Path) -> None:
    """Server/.env の「名前=値」を環境変数に入れる。すでに入っている値は上書きしない。"""
    if not path.exists():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        name, value = line.split("=", 1)
        os.environ.setdefault(name.strip(), value.strip())


load_env_file(HERE / ".env")

MODEL = "claude-opus-5-5"
LLM_DIR = pathlib.Path(os.environ.get("LLM_DIR", HERE.parent / "Assets" / "_Project" / "Stage" / "Llm"))
APP_TOKEN = os.environ.get("APP_TOKEN", "")
DAILY_LIMIT = int(os.environ.get("DAILY_LIMIT", "100"))
# キーがないとき、または MOCK=1 のときは Claude を呼ばず、決まった答えを返す
MOCK = os.environ.get("MOCK") == "1" or not os.environ.get("ANTHROPIC_API_KEY")
MAX_PHOTO_BYTES = 5 * 1024 * 1024

# 本物のキーで動かすときは、合言葉を必ず決める（合言葉なしで誰でも API の料金を使えないように）
if not MOCK and not APP_TOKEN:
    raise RuntimeError("ANTHROPIC_API_KEY を入れたときは、.env の APP_TOKEN に合言葉も入れてください")

PROMPT = (LLM_DIR / "scan_prompt.md").read_text(encoding="utf-8")
SCHEMA = json.loads((LLM_DIR / "scan_result.schema.json").read_text(encoding="utf-8"))

# 試しのモードの答え（Docs/LlmScanFormat.md の「試した結果 その1」と同じ）
MOCK_RESULT = {
    "paperFound": False,
    "scaleReference": "マーカーの箱の横幅を約19.5cmとした",
    "desk": None,
    "blocks": [
        {"label": "プロッキーの箱", "centerXCm": 16.3, "centerZCm": 6.5, "widthCm": 19.5, "depthCm": 13.0,
         "heightCm": 2.0, "yawDegrees": 0, "confidence": 0.6},
        {"label": "細長い段ボール箱", "centerXCm": 20.0, "centerZCm": 18.5, "widthCm": 30.0, "depthCm": 11.0,
         "heightCm": 4.0, "yawDegrees": 0, "confidence": 0.5},
        {"label": "大きな段ボール箱", "centerXCm": 23.0, "centerZCm": 40.0, "widthCm": 25.0, "depthCm": 24.0,
         "heightCm": 24.0, "yawDegrees": 0, "confidence": 0.4},
    ],
    "notes": "試しのモードの決まった答え",
}

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
log = logging.getLogger("scan")

app = FastAPI(title="DeskStage scan server")
client = None if MOCK else anthropic.Anthropic()

# 写真2枚と、フォームの区切りの分の上限。これより大きいと言うリクエストは、受け取る前に断る
MAX_REQUEST_BYTES = 2 * MAX_PHOTO_BYTES + 64 * 1024


@app.middleware("http")
async def limit_request_size(request: Request, call_next):
    length = request.headers.get("content-length")
    if length is not None and (not length.isdigit() or int(length) > MAX_REQUEST_BYTES):
        return JSONResponse(status_code=413, content={"detail": "送られたデータが大きすぎます"})
    return await call_next(request)


class DailyCounter:
    """1日あたりの読み取りの回数を数える（API の料金の使いすぎを防ぐ）。サーバーを止めると0に戻る。"""

    def __init__(self, limit: int):
        self.limit = limit
        self.day = None
        self.count = 0
        self.lock = threading.Lock()

    def try_take(self) -> bool:
        with self.lock:
            today = datetime.date.today()
            if self.day != today:
                self.day = today
                self.count = 0
            if self.count >= self.limit:
                return False
            self.count += 1
            return True


counter = DailyCounter(DAILY_LIMIT)


@app.get("/health")
def health():
    return {"ok": True, "mock": MOCK, "model": MODEL}


@app.post("/scan")
def scan(
    top: UploadFile = File(..., description="真上から撮った写真（JPEG）"),
    side: UploadFile = File(..., description="手前から斜めに撮った写真（JPEG）"),
    x_app_token: str = Header(default=""),
):
    # 合言葉が違うリクエストは断る。合言葉なしで通すのは、試しのモードで APP_TOKEN を決めていないときだけ
    if APP_TOKEN or not MOCK:
        if not hmac.compare_digest(x_app_token.encode("utf-8"), APP_TOKEN.encode("utf-8")):
            raise HTTPException(status_code=401, detail="合言葉が違います")

    photos = [read_photo(top, "top"), read_photo(side, "side")]

    if MOCK:
        log.info("scan: mock")
        return MOCK_RESULT

    if not counter.try_take():
        raise HTTPException(status_code=429, detail=f"今日の読み取りの上限（{DAILY_LIMIT}回）を超えました")

    return ask_claude(photos)


def read_photo(upload: UploadFile, name: str) -> bytes:
    # 少しずつ読み、上限を超えたらすぐにやめる（大きなファイルでメモリを使い切らないように）
    data = bytearray()
    while chunk := upload.file.read(64 * 1024):
        data.extend(chunk)
        if len(data) > MAX_PHOTO_BYTES:
            raise HTTPException(status_code=413, detail=f"{name} の写真が大きすぎます（5MB まで）")
    if not data:
        raise HTTPException(status_code=400, detail=f"{name} の写真が空です")
    return bytes(data)


def image_block(data: bytes) -> dict:
    return {
        "type": "image",
        "source": {"type": "base64", "media_type": "image/jpeg", "data": base64.standard_b64encode(data).decode("ascii")},
    }


def ask_claude(photos: list) -> dict:
    try:
        response = client.beta.messages.create(
            model=MODEL,
            max_tokens=16000,
            system=PROMPT,
            messages=[{
                "role": "user",
                "content": [
                    {"type": "text", "text": "1枚目：真上から撮った写真"},
                    image_block(photos[0]),
                    {"type": "text", "text": "2枚目：手前から斜めに撮った写真"},
                    image_block(photos[1]),
                    {"type": "text", "text": "この机に並べた物を、指示のとおりに読み取ってください。"},
                ],
            }],
            output_config={
                "effort": "medium",
                "format": {"type": "json_schema", "schema": SCHEMA},
            },
            # 答えを断られたとき、Anthropic がすすめる別のモデルで答え直す
            betas=["server-side-fallback-2026-07-01"],
            fallbacks="default",
        )
    except anthropic.RateLimitError:
        log.warning("scan: rate limited")
        raise HTTPException(status_code=503, detail="混んでいます。少し待ってから撮り直してください")
    except anthropic.APIStatusError as e:
        log.error("scan: API error %s", e.status_code)
        raise HTTPException(status_code=502, detail=f"Claude の API でエラーが起きました（{e.status_code}）")
    except anthropic.APIConnectionError:
        log.error("scan: connection error")
        raise HTTPException(status_code=502, detail="Claude の API につながりませんでした")

    # 使ったトークンの量だけをログに残す（料金の目安。写真は残さない）
    log.info("scan: model=%s stop=%s input=%s output=%s",
             response.model, response.stop_reason, response.usage.input_tokens, response.usage.output_tokens)

    if response.stop_reason == "refusal":
        raise HTTPException(status_code=502, detail="Claude が答えを断りました。写真を撮り直してください")
    if response.stop_reason == "max_tokens":
        raise HTTPException(status_code=502, detail="答えが長すぎて途中で切れました")

    text = next((b.text for b in response.content if b.type == "text"), None)
    if text is None:
        raise HTTPException(status_code=502, detail="答えが空でした")
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        raise HTTPException(status_code=502, detail="答えが JSON の形になっていませんでした")
