"""
机の写真を Claude に渡して、物ごとの位置と大きさを JSON で返すサーバー（A の Step 24）。

- POST /scan：写真2枚（top＝真上、side＝斜め）を受け取り、答えの JSON を返す
- 答えの形は B のスキーマ（Assets/_Project/Stage/Llm/scan_result.schema.json）で固定する
- API キーはこのサーバーにだけ置く。写真は保存しないし、ログにも書かない（00_overview.md 9.5）
- POST /stages・GET /stages・GET /stages/{id}：みんなの ステージの投稿・一覧・1 件
  （Docs/specs/home-modes の REQ-9〜12。写真は受け取らない。投稿の中身はログに書かない）
"""

import base64
import datetime
import hmac
import json
import logging
import math
import os
import pathlib
import re
import secrets
import string
import threading
import unicodedata

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


def check_token(x_app_token: str) -> None:
    """合言葉が違うリクエストは断る。合言葉なしで通すのは、試しのモードで APP_TOKEN を決めていないときだけ。"""
    if APP_TOKEN or not MOCK:
        if not hmac.compare_digest(x_app_token.encode("utf-8"), APP_TOKEN.encode("utf-8")):
            raise HTTPException(status_code=401, detail="合言葉が違います")


@app.post("/scan")
def scan(
    top: UploadFile = File(..., description="真上から撮った写真（JPEG）"),
    side: UploadFile = File(..., description="手前から斜めに撮った写真（JPEG）"),
    x_app_token: str = Header(default=""),
):
    check_token(x_app_token)

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


# ---- みんなの ステージ（Docs/specs/home-modes の REQ-9〜12） ----
# 投稿は、この Mac の SHARED_STAGES_DIR に 1 件 1 ファイル（<サーバーが決めた id>.json）で置く。
# 写真は受け取らない。投稿の中身はログに書かない（残すのは id と件数だけ）。

SHARED_STAGES_DIR = pathlib.Path(os.environ.get("SHARED_STAGES_DIR") or HERE / "shared_stages")
MAX_STAGE_BYTES = 200 * 1024  # 投稿 1 件の大きさの上限
MAX_SHARED_STAGES = 500  # 置いておける投稿の数の上限
MAX_STAGE_LIST = 100  # 一覧に出す数の上限
MAX_BOXES = 100
MAX_CHECKPOINTS = 5  # StageData.MaxCheckpoints と同じ
MAX_NAME_CHARS = 30
DEFAULT_STAGE_NAME = "ステージ"
MAX_LENGTH_M = 2.0  # 位置・机の大きさの上限（m）
MAX_BOX_SIZE_M = 1.0
MAX_RADIUS_M = 0.5
MAX_FALL_DEPTH_M = 1.0
MAX_TIME_LIMIT_SECONDS = 600.0
MAX_JUMPS = 99
DEFAULT_FALL_DEPTH_M = 0.10  # StageData.fallDepth の最初の値

# 1 件の id の決まり。パスに使う前に、この形かどうかを必ず確かめる（保存先の外を読めないように）
STAGE_ID_PATTERN = re.compile(r"c-[A-Za-z0-9-]{1,64}")
BOX_ID_PATTERN = re.compile(r"[A-Za-z0-9_-]{1,32}")
BOX_SOURCES = ("auto", "manual")
BOX_GIMMICKS = ("", "water", "fire", "heal")
ID_CHARS = string.ascii_lowercase + string.digits

stage_lock = threading.Lock()


class StageError(ValueError):
    """投稿の形が違う。どの項目が違うかだけを伝える（送られた値はそのまま返さない）。"""


def check_number(value, where: str, low: float, high: float, low_open: bool = False) -> float:
    """有限の数で、low〜high の中か（low_open なら low ちょうどは断る）を確かめる。"""
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise StageError(f"{where} は数にしてください")
    value = float(value)
    if not math.isfinite(value):
        raise StageError(f"{where} は有限の数にしてください")
    if value < low or value > high or (low_open and value <= low):
        raise StageError(f"{where} の値が範囲の外です")
    return value


def check_int(value, where: str, low: int, high: int) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise StageError(f"{where} は整数にしてください")
    if value < low or value > high:
        raise StageError(f"{where} の値が範囲の外です")
    return value


def check_bool(value, where: str) -> bool:
    if not isinstance(value, bool):
        raise StageError(f"{where} は true か false にしてください")
    return value


def check_object(value, where: str) -> dict:
    if not isinstance(value, dict):
        raise StageError(f"{where} はオブジェクトにしてください")
    return value


def check_list(value, where: str, max_count: int) -> list:
    if not isinstance(value, list):
        raise StageError(f"{where} は配列にしてください")
    if len(value) > max_count:
        raise StageError(f"{where} は {max_count} 個までです")
    return value


def check_vec3(value, where: str, low: float, high: float, low_open: bool = False) -> list:
    if not isinstance(value, list) or len(value) != 3:
        raise StageError(f"{where} は長さ 3 の配列にしてください")
    return [check_number(v, f"{where}[{i}]", low, high, low_open) for i, v in enumerate(value)]


def require(obj: dict, key: str, where: str):
    if key not in obj:
        raise StageError(f"{where}.{key} がありません")
    return obj[key]


def clean_stage_name(value) -> str:
    """名前から制御文字などを除き、30 文字までに切る。空なら「ステージ」にする。"""
    if value is None:
        return DEFAULT_STAGE_NAME
    if not isinstance(value, str):
        raise StageError("name は文字列にしてください")
    # Cc（制御文字）・Cf（見えない書式の文字）・Cs（対になっていないサロゲート）・行と段落の区切りを除く
    kept = "".join(c for c in value if unicodedata.category(c) not in ("Cc", "Cf", "Cs", "Zl", "Zp"))
    kept = kept.strip()[:MAX_NAME_CHARS].strip()
    return kept or DEFAULT_STAGE_NAME


def build_goal(value, where: str) -> dict:
    goal = check_object(value, where)
    return {
        "position": check_vec3(require(goal, "position", where), f"{where}.position", -MAX_LENGTH_M, MAX_LENGTH_M),
        "radius": check_number(require(goal, "radius", where), f"{where}.radius", 0.0, MAX_RADIUS_M),
    }


def build_box(value, where: str, index: int, used_ids: set) -> dict:
    box = check_object(value, where)
    box_id = box.get("id", "")
    # 箱の id は、決まった形で重ならないときだけ残す。そうでなければサーバーが付け直す
    if not isinstance(box_id, str) or not BOX_ID_PATTERN.fullmatch(box_id) or box_id in used_ids:
        box_id = f"box-{index + 1:02d}"
        while box_id in used_ids:
            box_id += "x"
    used_ids.add(box_id)

    source = box.get("source", "auto")
    if source not in BOX_SOURCES:
        raise StageError(f"{where}.source は auto か manual にしてください")
    gimmick = box.get("gimmick", "")
    if gimmick not in BOX_GIMMICKS:
        raise StageError(f"{where}.gimmick は \"\"・water・fire・heal のどれかにしてください")

    return {
        "id": box_id,
        "center": check_vec3(require(box, "center", where), f"{where}.center", -MAX_LENGTH_M, MAX_LENGTH_M),
        "size": check_vec3(require(box, "size", where), f"{where}.size", 0.0, MAX_BOX_SIZE_M, low_open=True),
        "yawDegrees": check_number(box.get("yawDegrees", 0.0), f"{where}.yawDegrees", -math.inf, math.inf),
        "source": source,
        "texture": "",  # 写真のファイル名は残さない
        "gimmick": gimmick,
        "flow": check_int(box.get("flow", 0), f"{where}.flow", 0, 4),
        "ramp": check_bool(box.get("ramp", False), f"{where}.ramp"),
    }


def build_rules(value) -> dict:
    """しばり。古い形の投稿には無いので、無ければ しばり無し にする。"""
    if value is None:
        value = {}
    rules = check_object(value, "rules")
    return {
        "maxJumps": check_int(rules.get("maxJumps", -1), "rules.maxJumps", -1, MAX_JUMPS),
        "noDoubleJump": check_bool(rules.get("noDoubleJump", False), "rules.noDoubleJump"),
        "oneHit": check_bool(rules.get("oneHit", False), "rules.oneHit"),
        "timeLimitSeconds": check_number(rules.get("timeLimitSeconds", 0.0), "rules.timeLimitSeconds",
                                         0.0, MAX_TIME_LIMIT_SECONDS),
    }


def build_stage(data, stage_id: str, created_at: str) -> dict:
    """送られた値を確かめ、決めた項目だけで組み立て直す（知らない項目は捨てる）。id と createdAt はサーバーが決める。"""
    stage = check_object(data, "ステージ")
    desk = check_object(require(stage, "desk", "stage"), "desk")
    spawn = check_object(require(stage, "spawn", "stage"), "spawn")
    boxes = check_list(require(stage, "boxes", "stage"), "boxes", MAX_BOXES)
    checkpoints = check_list(stage.get("checkpoints", []), "checkpoints", MAX_CHECKPOINTS)

    used_ids = set()
    return {
        "version": check_int(require(stage, "version", "stage"), "version", 1, 99),
        "id": stage_id,
        "name": clean_stage_name(stage.get("name")),
        "createdAt": created_at,
        "desk": {
            "width": check_number(require(desk, "width", "desk"), "desk.width", 0.0, MAX_LENGTH_M, low_open=True),
            "depth": check_number(require(desk, "depth", "desk"), "desk.depth", 0.0, MAX_LENGTH_M, low_open=True),
        },
        "boxes": [build_box(b, f"boxes[{i}]", i, used_ids) for i, b in enumerate(boxes)],
        "spawn": {
            "position": check_vec3(require(spawn, "position", "spawn"), "spawn.position", -MAX_LENGTH_M, MAX_LENGTH_M),
            "yawDegrees": check_number(spawn.get("yawDegrees", 0.0), "spawn.yawDegrees", -math.inf, math.inf),
        },
        "goal": build_goal(require(stage, "goal", "stage"), "goal"),
        "checkpoints": [build_goal(c, f"checkpoints[{i}]") for i, c in enumerate(checkpoints)],
        "fallDepth": check_number(stage.get("fallDepth", DEFAULT_FALL_DEPTH_M), "fallDepth", 0.0, MAX_FALL_DEPTH_M),
        "rules": build_rules(stage.get("rules")),
    }


def has_rules(rules: dict) -> bool:
    return (rules.get("maxJumps", -1) != -1 or bool(rules.get("noDoubleJump")) or bool(rules.get("oneHit"))
            or (rules.get("timeLimitSeconds") or 0) > 0)


def new_stage_id(now: datetime.datetime) -> str:
    """c-＋日時（マイクロ秒まで）＋乱数の英数字。日時が先頭なので、名前の順が新しさの順になる。"""
    suffix = "".join(secrets.choice(ID_CHARS) for _ in range(8))
    return f"c-{now:%Y%m%d%H%M%S%f}-{suffix}"


def stage_path(stage_id: str) -> pathlib.Path:
    """id から保存先のファイルを決める。決まりに合わない id、保存先の外を指す id は 400 で断る。"""
    if not isinstance(stage_id, str) or not STAGE_ID_PATTERN.fullmatch(stage_id):
        raise HTTPException(status_code=400, detail="ステージの id の形が違います")
    root = SHARED_STAGES_DIR.resolve()
    path = (root / f"{stage_id}.json").resolve()
    if path.parent != root:
        raise HTTPException(status_code=400, detail="ステージの id の形が違います")
    return path


def stored_stage_ids() -> list:
    """保存してある投稿の id を、新しい順に返す。"""
    if not SHARED_STAGES_DIR.is_dir():
        return []
    ids = [p.stem for p in SHARED_STAGES_DIR.glob("c-*.json") if STAGE_ID_PATTERN.fullmatch(p.stem)]
    return sorted(ids, reverse=True)


async def read_stage_body(request: Request) -> bytes:
    # 少しずつ読み、200KB を超えたらすぐにやめる（全体の上限 MAX_REQUEST_BYTES とは別に確かめる）
    length = request.headers.get("content-length")
    if length is not None and length.isdigit() and int(length) > MAX_STAGE_BYTES:
        raise HTTPException(status_code=413, detail="ステージのデータが大きすぎます（200KB まで）")
    data = bytearray()
    async for chunk in request.stream():
        data.extend(chunk)
        if len(data) > MAX_STAGE_BYTES:
            raise HTTPException(status_code=413, detail="ステージのデータが大きすぎます（200KB まで）")
    return bytes(data)


def reject_constant(name: str):
    raise ValueError("NaN や Infinity は使えません")


def parse_stage_json(body: bytes):
    try:
        return json.loads(body.decode("utf-8"), parse_constant=reject_constant)
    except (UnicodeDecodeError, ValueError, RecursionError):
        raise HTTPException(status_code=400, detail="ステージのデータが JSON の形になっていません")


@app.post("/stages")
async def post_stage(request: Request, x_app_token: str = Header(default="")):
    check_token(x_app_token)
    data = parse_stage_json(await read_stage_body(request))
    if not isinstance(data, dict):
        raise HTTPException(status_code=400, detail="ステージのデータは JSON のオブジェクトにしてください")

    now = datetime.datetime.now().astimezone()
    stage_id = new_stage_id(now)
    try:
        stage = build_stage(data, stage_id, now.isoformat(timespec="seconds"))
    except StageError as e:
        raise HTTPException(status_code=400, detail=str(e))

    with stage_lock:
        count = len(stored_stage_ids())
        if count >= MAX_SHARED_STAGES:
            log.warning("stages: full count=%d", count)
            raise HTTPException(status_code=507, detail=f"投稿の数が上限（{MAX_SHARED_STAGES}件）に達しました")
        save_stage(stage_id, stage)

    log.info("stages: saved id=%s count=%d", stage_id, count + 1)
    return {"id": stage_id}


def save_stage(stage_id: str, stage: dict) -> None:
    # 書きかけのファイルを読まれないよう、別の名前で書いてから置き換える
    path = stage_path(stage_id)
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(f".{stage_id}.tmp")
    temp.write_text(json.dumps(stage, ensure_ascii=False, allow_nan=False), encoding="utf-8")
    os.replace(temp, path)


def load_stage(path: pathlib.Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


@app.get("/stages")
def list_stages(x_app_token: str = Header(default="")):
    check_token(x_app_token)
    summaries = []
    for stage_id in stored_stage_ids()[:MAX_STAGE_LIST]:
        try:
            stage = load_stage(stage_path(stage_id))
            summaries.append({
                "id": stage_id,
                "name": stage.get("name", DEFAULT_STAGE_NAME),
                "createdAt": stage.get("createdAt", ""),
                "boxCount": len(stage.get("boxes", [])),
                "hasRules": has_rules(stage.get("rules") or {}),
            })
        except (OSError, ValueError, AttributeError, TypeError):
            log.warning("stages: unreadable id=%s", stage_id)
    return {"stages": summaries}


@app.get("/stages/{stage_id}")
def get_stage(stage_id: str, x_app_token: str = Header(default="")):
    check_token(x_app_token)
    path = stage_path(stage_id)
    if not path.is_file():
        raise HTTPException(status_code=404, detail="そのステージはありません")
    try:
        return load_stage(path)
    except (OSError, ValueError):
        log.warning("stages: unreadable id=%s", stage_id)
        raise HTTPException(status_code=500, detail="ステージを読めませんでした")
