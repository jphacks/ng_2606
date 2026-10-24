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
    }
}
