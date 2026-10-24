using NUnit.Framework;
using UnityEngine;

namespace DeskStage.UI.Tests
{
    public class TwoFingerGestureTests
    {
        [Test]
        public void 少しの動きではまだ決めない()
        {
            var g = new TwoFingerGesture();
            g.Begin(new Vector2(0, 0), new Vector2(100, 0));
            g.Update(new Vector2(0, 0), new Vector2(104, 2));
            Assert.AreEqual(TwoFingerGesture.Mode.Undecided, g.Current);
            Assert.AreEqual(1f, g.Scale);
            Assert.AreEqual(0f, g.Angle);
        }

        [Test]
        public void 広げるとつまむに決まり倍率が出る()
        {
            var g = new TwoFingerGesture();
            g.Begin(new Vector2(0, 0), new Vector2(100, 0));
            g.Update(new Vector2(0, 0), new Vector2(150, 0));
            Assert.AreEqual(TwoFingerGesture.Mode.Pinch, g.Current);
            Assert.AreEqual(1.5f, g.Scale, 1e-4f);
            Assert.AreEqual(0f, g.Angle);
        }

        [Test]
        public void ひねるとひねるに決まり_あとから広げても倍率は変わらない()
        {
            var g = new TwoFingerGesture();
            g.Begin(new Vector2(0, 0), new Vector2(100, 0));
            // 画面の上で反時計回りに30度
            var rotated = new Vector2(Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad)) * 100f;
            g.Update(Vector2.zero, rotated);
            Assert.AreEqual(TwoFingerGesture.Mode.Twist, g.Current);
            Assert.AreEqual(-30f, g.Angle, 1e-3f);
            g.Update(Vector2.zero, rotated * 2f);
            Assert.AreEqual(1f, g.Scale);
        }
    }
}
