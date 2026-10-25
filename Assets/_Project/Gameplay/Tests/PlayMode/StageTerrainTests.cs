using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeskStage.Gameplay.PlayTests
{
    /// <summary>
    /// 箱の地形での確かめ（C_gameplay.md の Step 10）。
    /// StreamingAssets/Stages の下のステージを全部読み、箱ごとに「立てる・歩いて降りられる・乗れる」を確かめる。
    /// 問題のあった箱の id をログに出す（B の Docs/StageIssues.md に書く材料）。
    /// </summary>
    public class StageTerrainTests
    {
        // 箱の上面に乗れたとみなす誤差（m）
        const float OnTopTolerance = 0.003f;
        // 立っているときの上下の揺れの上限（m）
        const float JitterLimit = 0.0002f;
        // 「進めない」とみなす、水平の速さの上限（m/秒）。歩く速さ 0.15 の 1/12。測る間隔は 0.05 秒
        const float StuckSpeed = 0.012f;
        const float StuckWindow = 0.05f;

        GameObject world;
        CharacterSettings settings;
        StepRules rules;
        CharacterMotor motor;
        CharacterController controller;

        static IEnumerable<string> StageIds()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Stages");
            if (!Directory.Exists(dir))
            {
                return Enumerable.Empty<string>();
            }
            return Directory.GetDirectories(dir)
                .Where(d => File.Exists(Path.Combine(d, "stage.json")))
                .Select(Path.GetFileName)
                .OrderBy(id => id);
        }

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<CharacterSettings>();
            rules = settings.CreateStepRules();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(world);
            Object.Destroy(settings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator 全部の箱に立てて降りられて乗れる([ValueSource(nameof(StageIds))] string stageId)
        {
            var stage = JsonUtility.FromJson<StageData>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Stages", stageId, "stage.json")));
            var boxes = BuildStage(stage);
            yield return null;
            yield return CreatePlayer();

            var issues = new List<string>();
            var notes = new List<string>();
            foreach (var box in stage.boxes)
            {
                yield return CheckStand(box, boxes[box.id], issues);
                yield return CheckWalkOff(box, boxes[box.id], issues);
                yield return CheckClimb(box, boxes[box.id], issues, notes);
            }

            var report = new StringBuilder($"[Step 10] {stageId}：箱 {stage.boxes.Length} 個、問題 {issues.Count} 件");
            foreach (var line in issues) report.Append("\n  問題 ").Append(line);
            foreach (var line in notes) report.Append("\n  お知らせ ").Append(line);
            Debug.Log(report.ToString());
            Assert.IsEmpty(issues, report.ToString());
        }

        // ---- 確かめること ----

        // 1：箱の上に置いて、沈まず浮かず震えずに立てるか
        IEnumerator CheckStand(BoxData box, Collider col, List<string> issues)
        {
            float top = Vec3.To(box.size).y;
            Vector3 center = Vec3.To(box.center);
            var hitCollider = TopAt(new Vector3(center.x, 0f, center.z), out _);
            if (hitCollider != col)
            {
                // 真ん中の上に別の箱が重なっている。立つ確かめはその箱で行う
                yield break;
            }

            motor.Teleport(new Vector3(center.x, top + 0.001f, center.z), Quaternion.identity);
            motor.SetTestInput(Vector2.zero);
            yield return new WaitForSeconds(0.4f);

            float min = float.MaxValue, max = float.MinValue;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float y = motor.transform.position.y;
                min = Mathf.Min(min, y);
                max = Mathf.Max(max, y);
                yield return null;
            }
            if (Mathf.Abs(min - top) > OnTopTolerance)
            {
                issues.Add($"{box.id}：上に立てない（足元 {min * 100f:0.0}cm、上面 {top * 100f:0.0}cm）");
            }
            else if (max - min > JitterLimit)
            {
                issues.Add($"{box.id}：上で立っていると震える（{(max - min) * 1000f:0.0}mm）");
            }
        }

        // 2：箱の上から4方向に歩いて、はさまったり、めり込んだりしないか
        IEnumerator CheckWalkOff(BoxData box, Collider col, List<string> issues)
        {
            float top = Vec3.To(box.size).y;
            Vector3 center = Vec3.To(box.center);
            if (TopAt(new Vector3(center.x, 0f, center.z), out _) != col)
            {
                yield break;
            }

            foreach (var dir in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
            {
                motor.Teleport(new Vector3(center.x, top + 0.001f, center.z), Quaternion.identity);
                motor.SetTestInput(Vector2.zero);
                yield return new WaitForSeconds(0.2f);

                motor.SetTestInput(new Vector2(dir.x, dir.z));
                // 進んだ量は、短い間（0.05 秒）ごとにまとめて測る。1 フレームごとに測ると、
                // フレームが速い環境（バッチ実行では 0.1 ミリ秒）で、進んでいても「進めない」と誤って判定してしまう
                Vector3 windowStart = motor.transform.position;
                float windowTime = 0f;
                float stuckTime = 0f;
                bool stuck = false, sunk = false;
                for (float t = 0f; t < 1.5f; t += Time.deltaTime)
                {
                    yield return null;
                    Vector3 pos = motor.transform.position;
                    if (Overlaps(pos))
                    {
                        sunk = true;
                    }
                    windowTime += Time.deltaTime;
                    if (windowTime < StuckWindow)
                    {
                        continue;
                    }
                    float speed = Vector3.Distance(Flat(pos), Flat(windowStart)) / windowTime;
                    windowStart = pos;
                    float elapsed = windowTime;
                    windowTime = 0f;
                    // 前が壁なら止まるのは正しい。壁がないのに進めないときだけ、はさまったとみなす
                    if (controller.isGrounded && speed < StuckSpeed && !WallAhead(pos, dir))
                    {
                        stuckTime += elapsed;
                        stuck |= stuckTime > 0.5f;
                    }
                    else
                    {
                        stuckTime = 0f;
                    }
                }
                motor.SetTestInput(Vector2.zero);
                string name = DirectionName(dir);
                if (sunk) issues.Add($"{box.id}：上から{name}へ歩くと、箱にめり込む");
                if (stuck) issues.Add($"{box.id}：上から{name}へ歩くと、壁がないのに進めない（はさまる）");
            }
        }

        // 3：まわりの低い面から、歩くかジャンプして乗れるか
        IEnumerator CheckClimb(BoxData box, Collider col, List<string> issues, List<string> notes)
        {
            Vector3 center = Vec3.To(box.center);
            Vector3 size = Vec3.To(box.size);
            float top = size.y;
            var rotation = Quaternion.Euler(0f, box.yawDegrees, 0f);

            bool anyCandidate = false;
            bool climbed = false;
            float lowestDiff = float.MaxValue;
            // 箱の4つの辺の真ん中の、すぐ外側から試す
            var sides = new[]
            {
                rotation * Vector3.forward * (size.z / 2f),
                rotation * Vector3.back * (size.z / 2f),
                rotation * Vector3.right * (size.x / 2f),
                rotation * Vector3.left * (size.x / 2f),
            };
            foreach (var side in sides)
            {
                Vector3 outward = side.normalized;
                Vector3 edge = center + side;
                Vector3 start = edge + outward * (settings.radius + 0.02f);
                var startCollider = TopAt(start, out float startY);
                if (startCollider == null || startCollider == col)
                {
                    continue;
                }
                float diff = top - startY;
                if (diff <= 0f)
                {
                    continue;
                }
                lowestDiff = Mathf.Min(lowestDiff, diff);
                if (rules.Classify(diff) == StepKind.Wall)
                {
                    continue;
                }
                var feet = new Vector3(start.x, startY + 0.001f, start.z);
                if (Overlaps(feet))
                {
                    // キャラが入る場所がない
                    continue;
                }
                anyCandidate = true;

                motor.Teleport(feet, Quaternion.LookRotation(-outward));
                motor.SetTestInput(Vector2.zero);
                yield return new WaitForSeconds(0.2f);
                yield return WalkJumpingWhenBlocked(-outward, 1.5f, top - OnTopTolerance);
                if (motor.transform.position.y > top - OnTopTolerance)
                {
                    climbed = true;
                    break;
                }
            }

            if (anyCandidate && !climbed)
            {
                issues.Add($"{box.id}：まわりから{(lowestDiff < rules.WalkMax ? "歩いて" : "ジャンプで")}乗れるはずなのに、乗れない");
            }
            else if (!anyCandidate && lowestDiff < float.MaxValue)
            {
                notes.Add($"{box.id}：どこからも乗れない（一番低い差 {lowestDiff * 100f:0.0}cm。決まりでは たかすぎる＝赤）");
            }
        }

        // ---- 手順 ----

        // 机の板と箱を作る。箱の id から Collider を引けるようにして返す
        Dictionary<string, Collider> BuildStage(StageData stage)
        {
            world = new GameObject("TestStage");
            CreateBox("desk", new Vector3(0f, -0.005f, 0f), new Vector3(stage.desk.width, 0.01f, stage.desk.depth), 0f);
            var map = new Dictionary<string, Collider>();
            foreach (var box in stage.boxes)
            {
                map[box.id] = CreateBox(box.id, Vec3.To(box.center), Vec3.To(box.size), box.yawDegrees);
            }
            return map;
        }

        Collider CreateBox(string name, Vector3 center, Vector3 size, float yaw)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = Layers.Terrain;
            go.transform.SetParent(world.transform, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = size;
            return go.GetComponent<Collider>();
        }

        IEnumerator CreatePlayer()
        {
            var go = new GameObject("Player");
            go.SetActive(false);
            go.layer = Layers.Player;
            go.transform.SetParent(world.transform, false);
            go.transform.position = new Vector3(0f, 1f, 0f);
            controller = go.AddComponent<CharacterController>();
            motor = go.AddComponent<CharacterMotor>();
            motor.settings = settings;
            go.SetActive(true);
            motor.SetTestInput(Vector2.zero);
            yield return null;
        }

        // 決めた向きへ歩き続け、地面にいて進めなくなったらジャンプを押す。stopAboveY より高い所に着いたら止まる
        IEnumerator WalkJumpingWhenBlocked(Vector3 dir, float seconds, float stopAboveY)
        {
            motor.SetTestInput(new Vector2(dir.x, dir.z));
            Vector3 lastPos = motor.transform.position;
            float stillTime = 0f;
            float nextJumpAllowed = 0f;
            float secondJumpAt = -1f;
            // 1 回のジャンプ（12cm + 足元で乗り上げる分）で届かない高さのときだけ、2 段ジャンプにする
            // （低い箱で 2 段にすると、箱を飛び越えてしまう）
            bool needTwoJumps = stopAboveY > 0.12f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (controller.isGrounded && motor.transform.position.y > stopAboveY)
                {
                    break;
                }
                yield return null;
                Vector3 pos = motor.transform.position;
                // 進んだ速さで見る（フレームが速くても、遅くても、同じ結果になる）
                float speed = Vector3.Distance(Flat(pos), Flat(lastPos)) / Mathf.Max(Time.deltaTime, 1e-4f);
                stillTime = speed < StuckSpeed ? stillTime + Time.deltaTime : 0f;
                lastPos = pos;
                if (controller.isGrounded && stillTime > 0.05f && Time.time >= nextJumpAllowed)
                {
                    motor.PressJumpForTest();
                    nextJumpAllowed = Time.time + 0.3f;
                    if (needTwoJumps) secondJumpAt = Time.time + 0.2f; // ジャンプの頂点のあたりで、もう一度押す（2 段ジャンプ）
                }
                if (secondJumpAt > 0f && Time.time >= secondJumpAt)
                {
                    if (!controller.isGrounded)
                    {
                        motor.PressJumpForTest();
                    }
                    secondJumpAt = -1f;
                }
            }
            motor.SetTestInput(Vector2.zero);
            yield return new WaitForSeconds(0.2f);
        }

        // 真上から見下ろして、一番上の面の高さと、その Collider を返す（なければ null）
        static Collider TopAt(Vector3 xz, out float y)
        {
            if (Physics.Raycast(new Vector3(xz.x, 1f, xz.z), Vector3.down, out var hit, 2f, Layers.TerrainMask, QueryTriggerInteraction.Ignore))
            {
                y = hit.point.y;
                return hit.collider;
            }
            y = float.NegativeInfinity;
            return null;
        }

        // キャラの体（少し細くしたもの）が箱に食い込んでいるか
        bool Overlaps(Vector3 feet)
        {
            float r = settings.radius - 0.002f;
            var bottom = feet + Vector3.up * (settings.radius + 0.001f);
            var topPoint = feet + Vector3.up * (settings.height - settings.radius);
            return Physics.CheckCapsule(bottom, topPoint, r, Layers.TerrainMask, QueryTriggerInteraction.Ignore);
        }

        // 進む向きのすぐ前に、登れない高さの壁があるか
        bool WallAhead(Vector3 feet, Vector3 dir)
        {
            var body = feet + Vector3.up * settings.centerY;
            return Physics.SphereCast(body, settings.radius * 0.9f, dir, out _, 0.01f, Layers.TerrainMask, QueryTriggerInteraction.Ignore);
        }

        static string DirectionName(Vector3 dir)
        {
            if (dir == Vector3.forward) return "奥（Z+）";
            if (dir == Vector3.back) return "手前（Z−）";
            if (dir == Vector3.right) return "右（X+）";
            return "左（X−）";
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
