using NUnit.Framework;
using UnityEngine;

namespace DeskStage.UI.Tests
{
    public class StepMarkLogicTests
    {
        static StepMarkLogic.Box B(float x, float height) => new StepMarkLogic.Box
        {
            Footprint = new Rect(x, 0f, 0.1f, 0.1f),
            Height = height,
        };

        [TestCase(0.005f, StepMarkLogic.Mark.Walk)]
        [TestCase(0.008f, StepMarkLogic.Mark.Jump)]
        [TestCase(0.22f, StepMarkLogic.Mark.Jump)]
        [TestCase(0.221f, StepMarkLogic.Mark.Wall)]
        [TestCase(-0.03f, StepMarkLogic.Mark.Jump)]
        public void 高さの差の判定はStepRulesと同じ(float diff, StepMarkLogic.Mark expected)
        {
            Assert.AreEqual(expected, StepMarkLogic.Classify(diff));
        }

        [Test]
        public void 三十センチの箱の横に何もないと赤()
        {
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.30f) });
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[0]);
        }

        [Test]
        public void 横に積み木を置くと緑になる()
        {
            // 30cm の箱の隣に 20cm の積み木（接している）
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.30f), B(0.1f, 0.20f) });
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[1]);
        }

        [Test]
        public void 離れた箱は隣にならない()
        {
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.30f), B(0.3f, 0.20f) });
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[0]);
        }

        [Test]
        public void 高い箱どうしを並べても机から登れなければ赤のまま()
        {
            // 30cm と 25cm：差は 5cm だが、どちらも机から 22cm を超えるので、たどり着けない
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.30f), B(0.1f, 0.25f) });
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[1]);
        }

        [Test]
        public void 階段にすると順にたどって登れる()
        {
            // 10cm → 20cm → 30cm と並べる：机から順に跳べる
            var marks = StepMarkLogic.Marks(new[] { B(0.2f, 0.30f), B(0.1f, 0.20f), B(0f, 0.10f) });
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[1]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[2]);
        }

        [Test]
        public void 登りやすい方の隣から判定する()
        {
            // 22.5cm の箱（机から 22cm を超える）の隣に、机から歩ける 0.5cm の板と、同じ高さの 22.5cm の箱（板から跳べる 22cm 差）
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.005f), B(0.1f, 0.225f), B(0.2f, 0.225f) });
            Assert.AreEqual(StepMarkLogic.Mark.Walk, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[1]);
            // 隣の 22.5cm の箱から歩いて移れる
            Assert.AreEqual(StepMarkLogic.Mark.Walk, marks[2]);
        }

        [Test]
        public void 低い箱は印なし()
        {
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.006f) });
            Assert.AreEqual(StepMarkLogic.Mark.Walk, marks[0]);
        }
    }
}
