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
        public void 高さ20cmの箱の上へは行けない()
        {
            var stage = Stage((0f, 0.20f));
            Assert.IsFalse(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0f, 0.20f, 0f)));
        }

        [Test]
        public void となりに10cmの箱があれば20cmの箱へ行ける()
        {
            // 10cm（x -8〜0cm）と 20cm（x 0〜8cm）がくっついている
            var stage = Stage((-0.04f, 0.10f), (0.04f, 0.20f));
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.04f, 0.20f, 0f)));
        }

        [Test]
        public void すき間が広いと20cmの箱へ行けない()
        {
            // 10cm（x -11〜-3cm）と 20cm（x 0〜8cm）の間に 3cm のすき間
            var stage = Stage((-0.07f, 0.10f), (0.04f, 0.20f));
            Assert.IsFalse(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.04f, 0.20f, 0f)));
        }

        [Test]
        public void すき間が狭ければつながっている()
        {
            // 10cm（x -9.5〜-1.5cm）と 20cm（x 0〜8cm）の間に 1.5cm のすき間
            var stage = Stage((-0.055f, 0.10f), (0.04f, 0.20f));
            Assert.IsTrue(CourseReachability.CanReach(stage, rules, OnDesk, new Vector3(0.04f, 0.20f, 0f)));
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
    }
}
