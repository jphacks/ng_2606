using NUnit.Framework;
using UnityEngine;

namespace DeskStage.UI.Tests
{
    public class BoxColorAssignerTests
    {
        static Rect R(float x, float z, float w, float d) => new Rect(x, z, w, d);

        [Test]
        public void 隣どうしは同じ色にならない()
        {
            // 横に3つ並んで接している
            var boxes = new[] { R(0f, 0f, 0.1f, 0.1f), R(0.1f, 0f, 0.1f, 0.1f), R(0.2f, 0f, 0.1f, 0.1f) };
            var c = BoxColorAssigner.Assign(boxes, 2, 0.01f);
            Assert.AreNotEqual(c[0], c[1]);
            Assert.AreNotEqual(c[1], c[2]);
        }

        [Test]
        public void 離れた箱は順に色がばらける()
        {
            var boxes = new[] { R(0f, 0f, 0.1f, 0.1f), R(0.5f, 0f, 0.1f, 0.1f), R(1f, 0f, 0.1f, 0.1f) };
            var c = BoxColorAssigner.Assign(boxes, 5, 0.01f);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, c);
        }

        [Test]
        public void 少しすき間があっても近ければ隣とみなす()
        {
            Assert.IsTrue(BoxColorAssigner.AreNeighbors(R(0f, 0f, 0.1f, 0.1f), R(0.105f, 0f, 0.1f, 0.1f), 0.01f));
            Assert.IsFalse(BoxColorAssigner.AreNeighbors(R(0f, 0f, 0.1f, 0.1f), R(0.13f, 0f, 0.1f, 0.1f), 0.01f));
        }

        [Test]
        public void 色が足りなければ範囲内の色を使う()
        {
            // 4つが互いに隣（田の字）で色は3つ
            var boxes = new[] { R(0f, 0f, 0.1f, 0.1f), R(0.1f, 0f, 0.1f, 0.1f), R(0f, 0.1f, 0.1f, 0.1f), R(0.1f, 0.1f, 0.1f, 0.1f) };
            var c = BoxColorAssigner.Assign(boxes, 3, 0.01f);
            foreach (var i in c)
            {
                Assert.That(i, Is.InRange(0, 2));
            }
            Assert.AreNotEqual(c[0], c[1]);
            Assert.AreNotEqual(c[0], c[2]);
        }
    }
}
