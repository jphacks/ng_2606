using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// スタートからゴールへ行けるかの調べ方（C_gameplay.md の Step 12 の 7）を確かめる。
    /// </summary>
    public class CourseReachabilityTests
    {
        readonly StepRules rules = new StepRules();

        // 机 60cm 四方に、幅・奥行き 8cm の箱を並べる。x は箱の中心
        static StageData Stage(params (float x, float height)[] boxes)
        {
            var stage = new StageData();
            stage.desk.width = 0.6f;
            stage.desk.depth = 0.6f;
            var list = new BoxData[boxes.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                list[i] = new BoxData
                {
                    id = $"box-{i + 1:00}",
                    center = Vec3.From(new Vector3(boxes[i].x, boxes[i].height / 2f, 0f)),
                    size = Vec3.From(new Vector3(0.08f, boxes[i].height, 0.08f)),
                };
            }
            stage.boxes = list;
            return stage;
        }

        static readonly Vector3 OnDesk = new Vector3(-0.25f, 0f, 0.2f);

        [Test]
        public void 机の上どうしは行ける()
        {
            Assert.IsTrue(CourseReachability.CanReach(Stage(), rules, OnDesk, new Vector3(0.2f, 0f, -0.2f)));
        }

        [Test]
        public void 高さ4cmの箱の上へは行ける()
        {
            var stage = Stage((0f, 0.04f));
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0f, 0.04f, 0f)));
        }

        [Test]
        public void 高さ30cmの箱の上へは行けない()
        {
            var stage = Stage((0f, 0.30f));
            Assert.IsFalse(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0f, 0.30f, 0f)));
        }

        [Test]
        public void となりに20cmの箱があれば30cmの箱へ行ける()
        {
            // 20cm（x -8〜0cm）と 30cm（x 0〜8cm）がくっついている
            var stage = Stage((-0.04f, 0.20f), (0.04f, 0.30f));
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.04f, 0.30f, 0f)));
        }

        [Test]
        public void すき間が広いと30cmの箱へ行けない()
        {
            // 20cm（x -11〜-3cm）と 30cm（x 0〜8cm）の間に 3cm のすき間
            var stage = Stage((-0.07f, 0.20f), (0.04f, 0.30f));
            Assert.IsFalse(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.04f, 0.30f, 0f)));
        }

        [Test]
        public void すき間が狭ければつながっている()
        {
            // 20cm（x -9.5〜-1.5cm）と 30cm（x 0〜8cm）の間に 1.5cm のすき間
            var stage = Stage((-0.055f, 0.20f), (0.04f, 0.30f));
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.04f, 0.30f, 0f)));
        }

        [Test]
        public void 高い所から降りるのはいつでも行ける()
        {
            var stage = Stage((0f, 0.14f));
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, new Vector3(0f, 0.14f, 0f), OnDesk));
        }

        [Test]
        public void どの面の上でもない点は判定しない()
        {
            var stage = Stage((0f, 0.08f));
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0f, 0.05f, 0f)));
        }

        [Test]
        public void 回した箱どうしの距離()
        {
            var a = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            var b = new[] { new Vector2(1.5f, 0), new Vector2(2.5f, 0), new Vector2(2.5f, 1), new Vector2(1.5f, 1) };
            Assert.AreEqual(0.5f, CourseReachability.Distance(a, b), 1e-5f);
            var c = new[] { new Vector2(0.5f, -0.5f), new Vector2(1.5f, 0.5f), new Vector2(0.5f, 1.5f), new Vector2(-0.5f, 0.5f) };
            Assert.AreEqual(0f, CourseReachability.Distance(a, c));
        }

        // 坂：中心 (0, 0, 0)、のぼる向きの長さ run、横はば 8cm、高い端の高さ height。yaw 0 なら +x の方へのぼる
        static StageData WithRamp(float run, float height, float yaw, string gimmick = "", int flow = 0)
        {
            var stage = new StageData();
            stage.desk.width = 0.6f;
            stage.desk.depth = 0.6f;
            stage.boxes = new[]
            {
                new BoxData
                {
                    id = "box-01",
                    center = Vec3.From(new Vector3(0f, height / 2f, 0f)),
                    size = Vec3.From(new Vector3(run, height, 0.08f)),
                    yawDegrees = yaw,
                    ramp = true,
                    gimmick = gimmick,
                    flow = flow,
                },
            };
            return stage;
        }

        [Test]
        public void ゆるやかな坂の高い端へ_壁の高さでも行ける()
        {
            // 高さ 30cm は、箱ならジャンプの上限（22cm）を超える壁。坂（傾き約 37 度）なら歩いてのぼれる
            var stage = WithRamp(0.40f, 0.30f, 0f);
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.19f, 0.2925f, 0f)));
        }

        [Test]
        public void 急な坂も_八十度まではジャンプでのぼれる()
        {
            // 長さ 10cm・高さ 30cm（傾き約 71 度）：歩いてはのぼれないが、跳べばのぼれる
            var stage = WithRamp(0.10f, 0.30f, 0f);
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.04f, 0.27f, 0f)));
        }

        [Test]
        public void 八十度より急な坂は_箱の壁と同じに扱う()
        {
            // 長さ 4cm・高さ 30cm（傾き約 82 度）
            var stage = WithRamp(0.04f, 0.30f, 0f);
            Assert.IsFalse(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.015f, 0.2625f, 0f)));
        }

        [Test]
        public void 水の坂は_机からのぼれない()
        {
            var stage = WithRamp(0.40f, 0.30f, 0f, BoxData.GimmickWater);
            Assert.IsFalse(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.19f, 0.2925f, 0f)));
        }

        [Test]
        public void 水の坂は_上から下りることはできる()
        {
            var stage = WithRamp(0.40f, 0.30f, 0f, BoxData.GimmickWater);
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, new Vector3(0.19f, 0.2925f, 0f), OnDesk));
        }

        [Test]
        public void 炎や回復の坂は_水でない坂と同じくのぼれる()
        {
            var stage = WithRamp(0.40f, 0.30f, 0f, BoxData.GimmickHeal);
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.19f, 0.2925f, 0f)));
        }

        [Test]
        public void 坂の向きは_回転に合わせて変わる()
        {
            // yaw 90：箱の +x は、ステージの -z を向く。高い端は z = -0.19 のあたり
            var stage = WithRamp(0.40f, 0.30f, 90f);
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0f, 0.2925f, -0.19f)));
        }

        [Test]
        public void 坂の斜めの面の上にいれば_その面の上にいるとみなす()
        {
            // 真ん中（高さは半分）にスタートを置いて、高い端のゴールへ
            var stage = WithRamp(0.40f, 0.30f, 0f);
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, new Vector3(0f, 0.15f, 0f), new Vector3(0.19f, 0.2925f, 0f)));
        }

        [Test]
        public void 水の坂は_下り向きを選んでも_のぼれない()
        {
            var stage = WithRamp(0.40f, 0.30f, 0f, BoxData.GimmickWater, 3);
            Assert.IsFalse(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.19f, 0.2925f, 0f)));
        }

        [Test]
        public void 水の坂は_上り向きにすると_流れに乗ってのぼれる()
        {
            var stage = WithRamp(0.40f, 0.30f, 0f, BoxData.GimmickWater, 1);
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.19f, 0.2925f, 0f)));
        }
    }
}
