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
        [TestCase(0.045f, StepMarkLogic.Mark.Jump)]
        [TestCase(0.046f, StepMarkLogic.Mark.Wall)]
        [TestCase(-0.03f, StepMarkLogic.Mark.Jump)]
        public void 高さの差の判定はStepRulesと同じ(float diff, StepMarkLogic.Mark expected)
        {
            Assert.AreEqual(expected, StepMarkLogic.Classify(diff));
        }

        [Test]
        public void 八センチの箱の横に何もないと赤()
        {
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.08f) });
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[0]);
        }

        [Test]
        public void 横に積み木を置くと緑になる()
        {
            // 8cm の箱の隣に 4cm の積み木（接している）
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.08f), B(0.1f, 0.04f) });
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[1]);
        }

        [Test]
        public void 離れた箱は隣にならない()
        {
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.08f), B(0.3f, 0.04f) });
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[0]);
        }

        [Test]
        public void 高い箱どうしを並べても机から登れなければ赤のまま()
        {
            // 8cm と 5cm：差は 3cm だが、どちらも机から 4.5cm を超えるので、たどり着けない
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.08f), B(0.1f, 0.05f) });
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Wall, marks[1]);
        }

        [Test]
        public void 階段にすると順にたどって登れる()
        {
            // 4cm → 8cm → 12cm と並べる：机から順に跳べる
            var marks = StepMarkLogic.Marks(new[] { B(0.2f, 0.12f), B(0.1f, 0.08f), B(0f, 0.04f) });
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[1]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[2]);
        }

        [Test]
        public void 登りやすい方の隣から判定する()
        {
            // 5cm の箱の隣に、机から歩ける 0.5cm の板と、同じ高さの 5cm の箱（板から跳べる 4.5cm 差）
            var marks = StepMarkLogic.Marks(new[] { B(0f, 0.005f), B(0.1f, 0.05f), B(0.2f, 0.05f) });
            Assert.AreEqual(StepMarkLogic.Mark.Walk, marks[0]);
            Assert.AreEqual(StepMarkLogic.Mark.Jump, marks[1]);
            // 隣の 5cm の箱から歩いて移れる
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
