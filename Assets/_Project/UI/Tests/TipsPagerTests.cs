using NUnit.Framework;

namespace DeskStage.UI.Tests
{
    public class TipsPagerTests
    {
        [Test]
        public void 左になぞると次の絵_右で前の絵()
        {
            var p = new TipsPager(3);
            Assert.IsTrue(p.EndSwipe(-200f, 1000f));
            Assert.AreEqual(1, p.Index);
            Assert.IsTrue(p.EndSwipe(200f, 1000f));
            Assert.AreEqual(0, p.Index);
        }

        [Test]
        public void 短いなぞりではめくらない()
        {
            var p = new TipsPager(3);
            Assert.IsFalse(p.EndSwipe(-50f, 1000f));
            Assert.AreEqual(0, p.Index);
        }

        [Test]
        public void 端より先にはめくれない()
        {
            var p = new TipsPager(2);
            Assert.IsFalse(p.EndSwipe(300f, 1000f));
            p.EndSwipe(-300f, 1000f);
            Assert.IsTrue(p.IsLast);
            Assert.IsFalse(p.EndSwipe(-300f, 1000f));
            Assert.AreEqual(1, p.Index);
        }

        [Test]
        public void 今の絵の印()
        {
            var p = new TipsPager(3);
            p.Go(1);
            Assert.AreEqual("○ ● ○", p.Dots());
        }
    }
}
