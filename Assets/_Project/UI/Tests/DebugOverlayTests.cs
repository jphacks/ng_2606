using NUnit.Framework;

namespace DeskStage.UI.Tests
{
    public class MultiTapDetectorTests
    {
        [Test]
        public void 三本指で二回たたくと成立する()
        {
            var d = new MultiTapDetector();
            Assert.IsFalse(d.Update(3, 0.0f));
            Assert.IsFalse(d.Update(0, 0.1f));
            Assert.IsTrue(d.Update(3, 0.3f));
        }

        [Test]
        public void 間があきすぎると成立しない()
        {
            var d = new MultiTapDetector();
            d.Update(3, 0.0f);
            d.Update(0, 0.1f);
            Assert.IsFalse(d.Update(3, 0.6f));
        }

        [Test]
        public void 指を置いたままでは数えない()
        {
            var d = new MultiTapDetector();
            d.Update(3, 0.0f);
            Assert.IsFalse(d.Update(3, 0.1f));
            Assert.IsFalse(d.Update(3, 0.2f));
        }

        [Test]
        public void 二本指では数えない()
        {
            var d = new MultiTapDetector();
            d.Update(2, 0.0f);
            d.Update(0, 0.1f);
            Assert.IsFalse(d.Update(2, 0.2f));
        }

        [Test]
        public void 成立したあとは数え直す()
        {
            var d = new MultiTapDetector();
            d.Update(3, 0.0f);
            d.Update(0, 0.1f);
            Assert.IsTrue(d.Update(3, 0.2f));
            d.Update(0, 0.25f);
            Assert.IsFalse(d.Update(3, 0.3f));
        }
    }

    public class DebugRegistryTests
    {
        [TearDown]
        public void TearDown() => DebugRegistry.Clear();

        [Test]
        public void 同じ名前は上書きし消せる()
        {
            DebugRegistry.AddValue("x", () => "1");
            DebugRegistry.AddValue("x", () => "2");
            Assert.AreEqual(1, DebugRegistry.Items.Count);
            Assert.AreEqual("2", DebugRegistry.Items[0].GetText());
            DebugRegistry.Remove("x");
            Assert.AreEqual(0, DebugRegistry.Items.Count);
        }
    }
}
