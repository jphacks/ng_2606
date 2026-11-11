using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    /// <summary>「いつもの こ」の顔の絵（DefaultFace）。</summary>
    public class DefaultFaceTests
    {
        static Color32 At(Color32[] pixels, int w, int h, float fx, float fy)
        {
            // 顔の枠の中の位置（-1〜1）から、画素を引く
            var f = HumanMask.Face;
            var x = Mathf.Clamp(Mathf.FloorToInt((f.x + f.z * fx) * w), 0, w - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt((f.y + f.w * fy) * h), 0, h - 1);
            return pixels[y * w + x];
        }

        static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;

        [Test]
        public void 撮った顔の写真と同じ縦横の比()
        {
            Assert.AreEqual(HumanMask.Aspect, (float)DefaultFace.Width / DefaultFace.Height, 0.01f);
        }

        [Test]
        public void 上は髪で真ん中は肌()
        {
            var w = DefaultFace.Width;
            var h = DefaultFace.Height;
            var pixels = DefaultFace.Pixels(w, h);
            Assert.IsTrue(Same((Color32)DefaultFace.HairColor, At(pixels, w, h, 0f, 0.8f)));
            Assert.IsTrue(Same(DefaultFace.Skin, At(pixels, w, h, 0f, -0.05f)));
        }

        [Test]
        public void 目は左右に一つずつある()
        {
            var w = DefaultFace.Width;
            var h = DefaultFace.Height;
            var pixels = DefaultFace.Pixels(w, h);
            foreach (var side in new[] { -1f, 1f })
            {
                // 目の下の方（白い光をよける）は、暗い
                var eye = At(pixels, w, h, side * 0.30f, -0.05f);
                Assert.Less(eye.r + eye.g + eye.b, 200, $"side {side}");
            }
        }

        [Test]
        public void 左右対称()
        {
            var w = DefaultFace.Width;
            var h = DefaultFace.Height;
            var pixels = DefaultFace.Pixels(w, h);
            for (var y = 0; y < h; y += 7)
            {
                for (var x = 0; x < w; x += 5)
                {
                    var a = pixels[y * w + x];
                    var b = pixels[y * w + (w - 1 - x)];
                    // 目の光だけは左寄りなので、明るさが大きく違う所は数えない程度に、ほぼ同じ
                    if (a.r == 255 && a.g == 255 || b.r == 255 && b.g == 255) continue;
                    Assert.LessOrEqual(Mathf.Abs(a.r - b.r), 40, $"({x}, {y})");
                }
            }
        }
    }
}
