using NUnit.Framework;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// 箱の高さの決まり（Docs/StepRules.md）の境目を確かめる。
    /// 0.8cm 未満は Walk、0.8〜4.5cm は Jump、4.5cm を超えると Wall。
    /// </summary>
    public class StepRulesTests
    {
        StepRules rules;

        [SetUp]
        public void SetUp()
        {
            rules = new StepRules();
        }

        [Test]
        public void 初期値は手順書の境目()
        {
            Assert.AreEqual(0.008f, rules.WalkMax);
            Assert.AreEqual(0.045f, rules.JumpMax);
        }

        [TestCase(0f)]
        [TestCase(0.005f)]
        [TestCase(0.0079f)]
        public void 差が0_8cm未満なら歩ける(float diff)
        {
            Assert.AreEqual(StepKind.Walk, rules.Classify(diff));
        }

        [TestCase(0.008f)]
        [TestCase(0.02f)]
        [TestCase(0.04f)]
        [TestCase(0.045f)]
        public void 差が0_8cmから4_5cmまでならジャンプ(float diff)
        {
            Assert.AreEqual(StepKind.Jump, rules.Classify(diff));
        }

        [TestCase(0.0451f)]
        [TestCase(0.05f)]
        [TestCase(0.08f)]
        public void 差が4_5cmを超えると壁(float diff)
        {
            Assert.AreEqual(StepKind.Wall, rules.Classify(diff));
        }

        [Test]
        public void 下りも差の大きさで決まる()
        {
            Assert.AreEqual(StepKind.Walk, rules.Classify(-0.005f));
            Assert.AreEqual(StepKind.Jump, rules.Classify(-0.02f));
            Assert.AreEqual(StepKind.Wall, rules.Classify(-0.08f));
        }

        [Test]
        public void 二つの面の高さから決まる()
        {
            // 2cm の箱から 6cm の箱へ：差 4cm でジャンプ
            Assert.AreEqual(StepKind.Jump, rules.Classify(0.02f, 0.06f));
            // 机の天面から 8cm の箱へ：壁
            Assert.AreEqual(StepKind.Wall, rules.Classify(0f, 0.08f));
        }

        [Test]
        public void 計算の誤差で境目がずれない()
        {
            // 5cm − 0.5cm を引き算で出すと 0.045 ちょうどにならないことがある
            Assert.AreEqual(StepKind.Jump, rules.Classify(0.005f, 0.05f));
            Assert.AreEqual(StepKind.Jump, rules.Classify(0.002f, 0.01f));
        }

        [Test]
        public void 境目は渡した値を使う()
        {
            var custom = new StepRules(0.01f, 0.03f);
            Assert.AreEqual(StepKind.Walk, custom.Classify(0.009f));
            Assert.AreEqual(StepKind.Jump, custom.Classify(0.03f));
            Assert.AreEqual(StepKind.Wall, custom.Classify(0.031f));
        }
    }
}
