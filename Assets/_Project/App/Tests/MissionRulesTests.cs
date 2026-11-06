using System.Collections.Generic;
using DeskStage.Shared;
using NUnit.Framework;

namespace DeskStage.App.Tests
{
    /// <summary>お題の判定（Docs/specs/home-modes の REQ-4・15）と、投稿の中身（REQ-9）。</summary>
    public class MissionRulesTests
    {
        static BoxData Box(float height, string gimmick = "", bool ramp = false)
            => new BoxData { size = new[] { 0.1f, height, 0.1f }, center = new[] { 0f, height / 2f, 0f }, gimmick = gimmick, ramp = ramp };

        static StageData Course(float sx, float sz, float gx, float gy, float gz)
        {
            var s = new StageData();
            s.spawn.position = new[] { sx, 0f, sz };
            s.goal.position = new[] { gx, gy, gz };
            s.goal.radius = 0.03f;
            return s;
        }

        static bool Met(MissionKind kind, List<BoxData> boxes, StageData stage = null, int count = 0, float meters = 0f, string gimmick = "")
            => MissionRules.IsMet(new MissionCondition { Kind = kind, Count = count, Meters = meters, Gimmick = gimmick }, boxes, stage);

        [Test]
        public void お題は8つで_条件は1つか2つ()
        {
            Assert.AreEqual(8, MissionCatalog.All.Length);
            foreach (var m in MissionCatalog.All)
            {
                Assert.That(m.Conditions.Length, Is.InRange(1, 2), m.Name);
                foreach (var c in m.Conditions) Assert.IsNotEmpty(c.Text, m.Name);
            }
        }

        [Test]
        public void 箱の数()
        {
            var four = new List<BoxData> { Box(0.02f), Box(0.04f), Box(0.06f), Box(0.08f) };
            Assert.IsFalse(Met(MissionKind.MinBoxes, four, count: 5));
            four.Add(Box(0.1f));
            Assert.IsTrue(Met(MissionKind.MinBoxes, four, count: 5));
            Assert.IsFalse(Met(MissionKind.MaxBoxes, four, count: 3));
            Assert.IsTrue(Met(MissionKind.MaxBoxes, new List<BoxData> { Box(0.1f) }, count: 3));
            Assert.IsFalse(Met(MissionKind.MaxBoxes, new List<BoxData>(), count: 3), "箱が 0 こは、少なく作ったことにしない");
        }

        [Test]
        public void しかけと坂()
        {
            var boxes = new List<BoxData> { Box(0.05f, BoxData.GimmickWater), Box(0.05f, ramp: true) };
            Assert.IsTrue(Met(MissionKind.Gimmick, boxes, gimmick: BoxData.GimmickWater));
            Assert.IsFalse(Met(MissionKind.Gimmick, boxes, gimmick: BoxData.GimmickFire));
            Assert.IsTrue(Met(MissionKind.Ramp, boxes));
            Assert.IsFalse(Met(MissionKind.Ramp, new List<BoxData> { Box(0.05f) }));
        }

        [Test]
        public void 高さ_箱とゴール()
        {
            var boxes = new List<BoxData> { Box(0.19f), Box(0.20f) };
            Assert.IsTrue(Met(MissionKind.BoxHeight, boxes, meters: 0.20f));
            Assert.IsFalse(Met(MissionKind.BoxHeight, new List<BoxData> { Box(0.19f) }, meters: 0.20f));
            // ゴールは、置くと面から 1mm 浮く
            Assert.IsTrue(Met(MissionKind.GoalHeight, boxes, Course(0.1f, 0f, 0f, 0.201f, 0f), meters: 0.20f));
            Assert.IsFalse(Met(MissionKind.GoalHeight, boxes, Course(0.1f, 0f, 0f, 0.15f, 0f), meters: 0.20f));
            Assert.IsFalse(Met(MissionKind.GoalHeight, boxes, new StageData(), meters: 0.0f), "ゴールを置いていない");
        }

        [Test]
        public void 段は_2cm以上ちがう高さを数え_坂は数えない()
        {
            var boxes = new List<BoxData> { Box(0.04f), Box(0.045f), Box(0.08f), Box(0.12f), Box(0.30f, ramp: true) };
            Assert.AreEqual(3, MissionRules.CountSteps(boxes));
            Assert.IsTrue(Met(MissionKind.Steps, boxes, count: 3));
            Assert.IsFalse(Met(MissionKind.Steps, boxes, count: 4));
        }

        [Test]
        public void スタートとゴールの距離()
        {
            var boxes = new List<BoxData> { Box(0.05f) };
            Assert.IsTrue(Met(MissionKind.Distance, boxes, Course(-0.2f, 0f, 0.2f, 0f, 0.05f), meters: 0.40f));
            Assert.IsFalse(Met(MissionKind.Distance, boxes, Course(-0.1f, 0f, 0.2f, 0f, 0f), meters: 0.40f));
            var noStart = Course(0f, 0f, 0.5f, 0f, 0f);
            Assert.IsFalse(Met(MissionKind.Distance, boxes, noStart, meters: 0.40f), "スタートを置いていない");
        }

        [Test]
        public void お題が無ければ_いつでも満たす_あれば全部の条件を見る()
        {
            Assert.IsTrue(MissionRules.AllMet(null, new List<BoxData>(), null));
            var both = MissionCatalog.All[2]; // 炎と回復
            Assert.IsFalse(MissionRules.AllMet(both, new List<BoxData> { Box(0.02f, BoxData.GimmickFire) }, null));
            Assert.IsTrue(MissionRules.AllMet(both, new List<BoxData> { Box(0.02f, BoxData.GimmickFire), Box(0.02f, BoxData.GimmickHeal) }, null));
        }

        [Test]
        public void 投稿の中身には_写真のファイル名を入れない()
        {
            var stage = new StageData { boxes = new[] { new BoxData { id = "box-01", texture = "top.jpg" } } };
            var json = StageShareClient.ToPostJson(stage);
            Assert.IsFalse(json.Contains("top.jpg"));
            Assert.AreEqual("top.jpg", stage.boxes[0].texture, "元のステージは変えない");
        }
    }
}
