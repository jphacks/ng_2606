"""
みんなの ステージの口（POST /stages・GET /stages・GET /stages/{id}）のテスト（Docs/specs/home-modes の REQ-12・15）。

標準の unittest と fastapi.testclient で動かす。Claude は呼ばない（MOCK=1、API キーは空）。外部への通信もしない。
    cd Server && python -m unittest -v test_stages
"""

import copy
import json
import os
import pathlib
import tempfile
import unittest
from unittest import mock

# app を読み込む前に環境変数を決める（.env の値より先に入れておけば、上書きされない）
_BASE_DIR = tempfile.TemporaryDirectory()
os.environ["MOCK"] = "1"
os.environ["APP_TOKEN"] = "test-token"
os.environ["ANTHROPIC_API_KEY"] = ""
os.environ["SHARED_STAGES_DIR"] = os.path.join(_BASE_DIR.name, "shared_stages")

from fastapi.testclient import TestClient  # noqa: E402

import app  # noqa: E402

TOKEN = {"X-App-Token": "test-token"}


def tearDownModule():
    _BASE_DIR.cleanup()


def sample_stage() -> dict:
    """アプリが送る形（StageData）の、正しいステージ。"""
    return {
        "version": 2,
        "id": "my-stage",
        "name": "つくえの やま",
        "createdAt": "2026-11-06T10:00:00+09:00",
        "desk": {"width": 1.2, "depth": 0.6},
        "boxes": [
            {"id": "box-01", "center": [0.1, 0.05, 0.0], "size": [0.2, 0.1, 0.15], "yawDegrees": 30.0,
             "source": "auto", "texture": "box-01.jpg", "gimmick": "", "flow": 0, "ramp": False},
            {"id": "box-02", "center": [-0.2, 0.02, 0.1], "size": [0.3, 0.04, 0.1], "yawDegrees": 0.0,
             "source": "manual", "texture": "photo_secret.png", "gimmick": "water", "flow": 2, "ramp": True},
        ],
        "spawn": {"position": [-0.5, 0.0, 0.0], "yawDegrees": 90.0},
        "goal": {"position": [0.1, 0.1, 0.0], "radius": 0.05},
        "checkpoints": [{"position": [-0.2, 0.04, 0.1], "radius": 0.04}],
        "fallDepth": 0.1,
        "rules": {"maxJumps": -1, "noDoubleJump": False, "oneHit": False, "timeLimitSeconds": 0.0},
    }


class StagesTest(unittest.TestCase):
    def setUp(self):
        # テストごとに空の保存先を使う
        self.temp = tempfile.TemporaryDirectory(dir=_BASE_DIR.name)
        self.root = pathlib.Path(self.temp.name) / "shared_stages"
        self.patch = mock.patch.object(app, "SHARED_STAGES_DIR", self.root)
        self.patch.start()
        self.client = TestClient(app.app)

    def tearDown(self):
        self.patch.stop()
        self.temp.cleanup()

    def post(self, stage, headers=TOKEN):
        return self.client.post("/stages", json=stage, headers=headers)

    def post_raw(self, text: str):
        return self.client.post("/stages", content=text.encode("utf-8"),
                                headers={**TOKEN, "Content-Type": "application/json"})

    def saved_files(self):
        return sorted(p.name for p in self.root.iterdir()) if self.root.exists() else []

    # ---- 合言葉 ----

    def test_token_missing_or_wrong_is_401(self):
        for headers in ({}, {"X-App-Token": "wrong"}):
            with self.subTest(headers=headers):
                self.assertEqual(self.post(sample_stage(), headers=headers).status_code, 401)
                self.assertEqual(self.client.get("/stages", headers=headers).status_code, 401)
                self.assertEqual(self.client.get("/stages/c-abc", headers=headers).status_code, 401)
        self.assertEqual(self.saved_files(), [])

    # ---- 正しい投稿 ----

    def test_post_then_list_and_get(self):
        res = self.post(sample_stage())
        self.assertEqual(res.status_code, 200)
        stage_id = res.json()["id"]
        self.assertRegex(stage_id, r"^c-[A-Za-z0-9-]{1,64}$")
        self.assertNotEqual(stage_id, "my-stage")
        self.assertEqual(self.saved_files(), [f"{stage_id}.json"])

        listed = self.client.get("/stages", headers=TOKEN)
        self.assertEqual(listed.status_code, 200)
        self.assertEqual(listed.json()["stages"], [{
            "id": stage_id, "name": "つくえの やま", "createdAt": listed.json()["stages"][0]["createdAt"],
            "boxCount": 2, "hasRules": False,
        }])

        got = self.client.get(f"/stages/{stage_id}", headers=TOKEN)
        self.assertEqual(got.status_code, 200)
        stage = got.json()
        self.assertEqual(stage["id"], stage_id)
        self.assertNotEqual(stage["createdAt"], "2026-11-06T10:00:00+09:00")
        self.assertEqual(stage["boxes"][1]["gimmick"], "water")
        self.assertEqual(stage["boxes"][1]["flow"], 2)
        self.assertTrue(stage["boxes"][1]["ramp"])
        self.assertEqual(stage["checkpoints"], [{"position": [-0.2, 0.04, 0.1], "radius": 0.04}])

    def test_texture_is_emptied(self):
        stage_id = self.post(sample_stage()).json()["id"]
        stage = self.client.get(f"/stages/{stage_id}", headers=TOKEN).json()
        self.assertEqual([b["texture"] for b in stage["boxes"]], ["", ""])
        self.assertNotIn("photo_secret", (self.root / f"{stage_id}.json").read_text(encoding="utf-8"))

    def test_unknown_fields_are_dropped(self):
        data = sample_stage()
        data["photo"] = "base64..."
        data["thumbnail"] = "thumb.png"
        data["boxes"][0]["secret"] = "x"
        data["rules"]["extra"] = 1
        data["desk"]["color"] = "red"
        stage_id = self.post(data).json()["id"]
        stage = self.client.get(f"/stages/{stage_id}", headers=TOKEN).json()
        self.assertEqual(set(stage), {"version", "id", "name", "createdAt", "desk", "boxes", "spawn", "goal",
                                      "checkpoints", "fallDepth", "rules"})
        self.assertEqual(set(stage["boxes"][0]), {"id", "center", "size", "yawDegrees", "source", "texture",
                                                  "gimmick", "flow", "ramp"})
        self.assertEqual(set(stage["rules"]), {"maxJumps", "noDoubleJump", "oneHit", "timeLimitSeconds"})
        self.assertEqual(set(stage["desk"]), {"width", "depth"})

    def test_sent_id_and_name_are_not_used_as_file_name(self):
        data = sample_stage()
        data["id"] = "../../evil"
        data["name"] = "../../evil"
        stage_id = self.post(data).json()["id"]
        self.assertEqual(self.saved_files(), [f"{stage_id}.json"])
        self.assertFalse((pathlib.Path(self.temp.name) / "evil").exists())

    def test_name_is_cleaned(self):
        cases = [
            ("あい\x00う\nえ​", "あいうえ"),
            ("あ" * 40, "あ" * 30),
            ("", "ステージ"),
            ("\x01\x02 ", "ステージ"),
            (None, "ステージ"),
        ]
        for name, expected in cases:
            with self.subTest(name=name):
                data = sample_stage()
                data["name"] = name
                stage_id = self.post(data).json()["id"]
                self.assertEqual(self.client.get(f"/stages/{stage_id}", headers=TOKEN).json()["name"], expected)

    def test_list_is_newest_first_and_has_rules(self):
        first = self.post(sample_stage()).json()["id"]
        data = sample_stage()
        data["rules"]["oneHit"] = True
        second = self.post(data).json()["id"]
        stages = self.client.get("/stages", headers=TOKEN).json()["stages"]
        self.assertEqual([s["id"] for s in stages], [second, first])
        self.assertEqual([s["hasRules"] for s in stages], [True, False])

    def test_list_is_at_most_100(self):
        with mock.patch.object(app, "MAX_STAGE_LIST", 2):
            for _ in range(3):
                self.post(sample_stage())
            self.assertEqual(len(self.client.get("/stages", headers=TOKEN).json()["stages"]), 2)
        self.assertEqual(app.MAX_STAGE_LIST, 100)

    def test_empty_list(self):
        self.assertEqual(self.client.get("/stages", headers=TOKEN).json(), {"stages": []})

    # ---- 形の確認 ----

    def test_nan_and_infinity_are_rejected(self):
        good = json.dumps(sample_stage())
        for bad in ("NaN", "Infinity", "-Infinity", "1e999"):
            with self.subTest(bad=bad):
                text = good.replace('"radius": 0.05', f'"radius": {bad}')
                self.assertNotEqual(text, good)
                self.assertEqual(self.post_raw(text).status_code, 400)
        self.assertEqual(self.saved_files(), [])

    def test_out_of_range_values_are_rejected(self):
        def edit(fn):
            data = sample_stage()
            fn(data)
            return data

        cases = {
            "center が 2m を超える": edit(lambda d: d["boxes"][0]["center"].__setitem__(0, 2.5)),
            "size が 0": edit(lambda d: d["boxes"][0]["size"].__setitem__(1, 0.0)),
            "size が 1m を超える": edit(lambda d: d["boxes"][0]["size"].__setitem__(0, 1.5)),
            "center の長さが 2": edit(lambda d: d["boxes"][0].__setitem__("center", [0.0, 0.0])),
            "size が文字列": edit(lambda d: d["boxes"][0].__setitem__("size", "big")),
            "radius が 0.5 を超える": edit(lambda d: d["goal"].__setitem__("radius", 0.6)),
            "fallDepth が 1 を超える": edit(lambda d: d.__setitem__("fallDepth", 1.5)),
            "maxJumps が 100": edit(lambda d: d["rules"].__setitem__("maxJumps", 100)),
            "maxJumps が -2": edit(lambda d: d["rules"].__setitem__("maxJumps", -2)),
            "制限時間が 601 秒": edit(lambda d: d["rules"].__setitem__("timeLimitSeconds", 601)),
            "gimmick が知らない物": edit(lambda d: d["boxes"][0].__setitem__("gimmick", "lava")),
            "flow が 5": edit(lambda d: d["boxes"][0].__setitem__("flow", 5)),
            "ramp が文字列": edit(lambda d: d["boxes"][0].__setitem__("ramp", "yes")),
            "spawn.position が範囲の外": edit(lambda d: d["spawn"].__setitem__("position", [0.0, -3.0, 0.0])),
            "チェックポイントが 6 個": edit(lambda d: d.__setitem__("checkpoints", [d["goal"]] * 6)),
            "goal が無い": edit(lambda d: d.pop("goal")),
            "boxes が配列でない": edit(lambda d: d.__setitem__("boxes", {})),
        }
        for label, data in cases.items():
            with self.subTest(label=label):
                self.assertEqual(self.post(data).status_code, 400)
        self.assertEqual(self.saved_files(), [])

    def test_box_count_limit(self):
        data = sample_stage()
        box = data["boxes"][0]
        data["boxes"] = [dict(copy.deepcopy(box), id=f"b{i}") for i in range(100)]
        self.assertEqual(self.post(data).status_code, 200)
        data["boxes"].append(copy.deepcopy(box))
        self.assertEqual(self.post(data).status_code, 400)

    def test_not_an_object_is_rejected(self):
        for text in ("[]", '"stage"', "1", "null", "{", "", "[" * 100000):
            with self.subTest(text=text[:10]):
                self.assertEqual(self.post_raw(text).status_code, 400)

    def test_too_large_body_is_rejected(self):
        data = sample_stage()
        data["padding"] = "x" * (app.MAX_STAGE_BYTES + 1)
        res = self.post(data)
        self.assertEqual(res.status_code, 413)
        self.assertEqual(self.saved_files(), [])

    # ---- id の確かめ ----

    def test_bad_ids_do_not_read_outside(self):
        # 保存先の外に置いたファイルは、どの id でも読めない
        outside = pathlib.Path(self.temp.name) / "x.json"
        outside.write_text('{"secret": true}', encoding="utf-8")
        self.root.mkdir(parents=True)
        for bad in ("../x", "..%2Fx", "c-..", "c-.%2E%2Fx", "x", "c-", "c-" + "a" * 65, "c-a_b", "c-a.json"):
            with self.subTest(bad=bad):
                res = self.client.get(f"/stages/{bad}", headers=TOKEN)
                self.assertIn(res.status_code, (400, 404))
                self.assertNotIn("secret", res.text)

    def test_bad_id_pattern_is_400(self):
        for bad in ("c-..", "c-" + "a" * 65, "x-abc", "c-a_b"):
            with self.subTest(bad=bad):
                self.assertEqual(self.client.get(f"/stages/{bad}", headers=TOKEN).status_code, 400)

    def test_missing_id_is_404(self):
        self.assertEqual(self.client.get("/stages/c-20260101-none", headers=TOKEN).status_code, 404)

    # ---- 件数の上限 ----

    def test_count_limit(self):
        with mock.patch.object(app, "MAX_SHARED_STAGES", 2):
            self.assertEqual(self.post(sample_stage()).status_code, 200)
            self.assertEqual(self.post(sample_stage()).status_code, 200)
            res = self.post(sample_stage())
            self.assertIn(res.status_code, (429, 507))
        self.assertEqual(len(self.saved_files()), 2)
        self.assertEqual(app.MAX_SHARED_STAGES, 500)


if __name__ == "__main__":
    unittest.main()
