using NUnit.Framework;
using UnityEngine;

namespace DeskStage.AR.Tests
{
    /// <summary>PhotoMath：写真を縮める大きさ（A の Step 22）。</summary>
    public class PhotoMathTests
    {
        [Test]
        public void 縦長の大きな写真は長い辺が上限になる()
        {
            var size = PhotoMath.FitLongSide(4284, 5712, 1568);

            Assert.That(size.y, Is.EqualTo(1568));
            Assert.That(size.x, Is.EqualTo(1176));
        }

        [Test]
        public void 横長の写真も縦横の比を保つ()
        {
            var size = PhotoMath.FitLongSide(1920, 1440, 1568);

            Assert.That(size, Is.EqualTo(new Vector2Int(1568, 1176)));
        }

        [Test]
        public void 小さい写真はそのまま()
        {
            Assert.That(PhotoMath.FitLongSide(1280, 960, 1568), Is.EqualTo(new Vector2Int(1280, 960)));
        }

        [Test]
        public void とても細長い写真でも辺が0にならない()
        {
            var size = PhotoMath.FitLongSide(10000, 1, 1568);

            Assert.That(size.x, Is.EqualTo(1568));
            Assert.That(size.y, Is.EqualTo(1));
        }

        [Test]
        public void 補正は0度なら何もしない()
        {
            Assert.IsTrue(PhotoMath.TryUprightTransform(0, false, out var scale, out var offset));
            Assert.AreEqual(new Vector2(1f, 1f), scale);
            Assert.AreEqual(Vector2.zero, offset);
        }

        [Test]
        public void 補正は180度なら上下左右を反転する()
        {
            Assert.IsTrue(PhotoMath.TryUprightTransform(180, false, out var scale, out var offset));
            Assert.AreEqual(new Vector2(-1f, -1f), scale);
            Assert.AreEqual(new Vector2(1f, 1f), offset);
        }

        [Test]
        public void 補正は上下反転の印があれば上下をもう一度反転する()
        {
            Assert.IsTrue(PhotoMath.TryUprightTransform(0, true, out var s0, out var o0));
            Assert.AreEqual(new Vector2(1f, -1f), s0);
            Assert.AreEqual(new Vector2(0f, 1f), o0);

            // 180 度 + 上下反転 = 左右だけ反転
            Assert.IsTrue(PhotoMath.TryUprightTransform(180, true, out var s1, out var o1));
            Assert.AreEqual(new Vector2(-1f, 1f), s1);
            Assert.AreEqual(new Vector2(1f, 0f), o1);
        }

        [Test]
        public void 補正は90度と270度を扱わない()
        {
            Assert.IsFalse(PhotoMath.TryUprightTransform(90, false, out var scale, out var offset));
            Assert.AreEqual(Vector2.one, scale);
            Assert.AreEqual(Vector2.zero, offset);
            Assert.IsFalse(PhotoMath.TryUprightTransform(270, false, out _, out _));
        }

        [Test]
        public void 補正は負の角度と360度以上も同じに扱う()
        {
            Assert.IsTrue(PhotoMath.TryUprightTransform(-180, false, out var scale, out _));
            Assert.AreEqual(new Vector2(-1f, -1f), scale);
            Assert.IsTrue(PhotoMath.TryUprightTransform(360, false, out var scale360, out _));
            Assert.AreEqual(Vector2.one, scale360);
        }
    }
}
