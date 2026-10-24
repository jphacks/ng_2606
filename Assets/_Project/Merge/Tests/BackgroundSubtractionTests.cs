using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    /// <summary>BackgroundSubtraction：背景だけの写真と、人形を置いた写真の差から、人形を取り出す。</summary>
    public class BackgroundSubtractionTests
    {
        const int W = 96;
        const int H = 128;

        /// <summary>模様のある（汚い）背景：なだらかな明暗に、決まった乱数の細かい模様を重ねる。</summary>
        static Color32[] DirtyBackground()
        {
            var rnd = new System.Random(7);
            var px = new Color32[W * H];
            for (var y = 0; y < H; y++)
            {
                for (var x = 0; x < W; x++)
                {
                    var baseValue = 90 + x * 80 / W + y * 40 / H;
                    // 5 画素ごとのまだら模様
                    var block = ((x / 5) * 7 + (y / 5) * 13) % 40;
                    var r = Mathf.Clamp(baseValue + block, 0, 255);
                    var g = Mathf.Clamp(baseValue + block / 2 + rnd.Next(6), 0, 255);
                    var b = Mathf.Clamp(baseValue - block / 2 + rnd.Next(6), 0, 255);
                    px[y * W + x] = new Color32((byte)r, (byte)g, (byte)b, 255);
                }
            }
            return px;
        }

        /// <summary>背景に、中心 (cx, cy)、半径 r の赤い円（人形）を置く。</summary>
        static Color32[] WithDoll(Color32[] background, float cx, float cy, float r)
        {
            var px = (Color32[])background.Clone();
            for (var y = 0; y < H; y++)
                for (var x = 0; x < W; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                        px[y * W + x] = new Color32(200, 40, 40, 255);
            return px;
        }

        /// <summary>写真を (dx, dy) 画素ずらす（手のぶれ）。</summary>
        static Color32[] Shifted(Color32[] src, int dx, int dy)
        {
            var px = new Color32[W * H];
            for (var y = 0; y < H; y++)
                for (var x = 0; x < W; x++)
                {
                    var sx = Mathf.Clamp(x - dx, 0, W - 1);
                    var sy = Mathf.Clamp(y - dy, 0, H - 1);
                    px[y * W + x] = src[sy * W + sx];
                }
            return px;
        }

        static Color32[] Brighter(Color32[] src, int amount)
        {
            var px = new Color32[src.Length];
            for (var i = 0; i < src.Length; i++)
                px[i] = new Color32((byte)Mathf.Clamp(src[i].r + amount, 0, 255), (byte)Mathf.Clamp(src[i].g + amount, 0, 255), (byte)Mathf.Clamp(src[i].b + amount, 0, 255), 255);
            return px;
        }

        static int OpaqueCount(CutoutResult r)
        {
            var n = 0;
            foreach (var p in r.Pixels) if (p.a == 255) n++;
            return n;
        }

        [Test]
        public void 汚い背景でも人形の形だけが取り出せる()
        {
            var bg = DirtyBackground();
            var radius = 0.17f * W;
            var photo = WithDoll(bg, 0.5f * W, 0.5f * H, radius);

            var r = BackgroundSubtraction.Cut(bg, photo, W, H);

            Assert.IsTrue(r.Ok, r.Reason);
            // 円の面積（約 π r²）の、9 割から 1 割増しの間（ふちのぼけを含む）
            var area = Mathf.PI * radius * radius;
            Assert.That(OpaqueCount(r), Is.InRange(area * 0.9f, area * 1.15f));
            // 切り抜きの大きさは、円の直径に近い
            Assert.That(r.Width, Is.InRange(2 * radius, 2 * radius + 8));
            Assert.That(r.Height, Is.InRange(2 * radius, 2 * radius + 8));
        }

        [Test]
        public void 手がぶれて少しずれても取り出せる()
        {
            var bg = DirtyBackground();
            var radius = 0.17f * W;
            var photo = Shifted(WithDoll(bg, 0.5f * W, 0.5f * H, radius), 3, -2);

            var r = BackgroundSubtraction.Cut(bg, photo, W, H);

            Assert.IsTrue(r.Ok, r.Reason);
            var area = Mathf.PI * radius * radius;
            Assert.That(OpaqueCount(r), Is.InRange(area * 0.8f, area * 1.3f));
        }

        [Test]
        public void 明るさが変わっても取り出せる()
        {
            var bg = DirtyBackground();
            var radius = 0.17f * W;
            var photo = Brighter(WithDoll(bg, 0.5f * W, 0.5f * H, radius), 14);

            var r = BackgroundSubtraction.Cut(bg, photo, W, H);

            Assert.IsTrue(r.Ok, r.Reason);
            var area = Mathf.PI * radius * radius;
            Assert.That(OpaqueCount(r), Is.InRange(area * 0.8f, area * 1.3f));
        }

        [Test]
        public void 同じ写真なら人形が見つからない()
        {
            var bg = DirtyBackground();
            var r = BackgroundSubtraction.Cut(bg, (Color32[])bg.Clone(), W, H);

            Assert.IsFalse(r.Ok);
            Assert.That(r.Reason, Does.Contain("みつかりません"));
        }

        [Test]
        public void 背景がまるで違うと失敗する()
        {
            var bg = DirtyBackground();
            var other = new Color32[W * H];
            var rnd = new System.Random(3);
            for (var i = 0; i < other.Length; i++)
                other[i] = new Color32((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256), 255);

            var r = BackgroundSubtraction.Cut(bg, other, W, H);

            Assert.IsFalse(r.Ok);
        }

        [Test]
        public void 人形の中の小さな穴は埋まる()
        {
            var bg = DirtyBackground();
            var radius = 0.2f * W;
            var photo = WithDoll(bg, 0.5f * W, 0.5f * H, radius);
            // 人形の中に、背景そのままの小さな穴（人形の色が背景に近い所）
            for (var y = 62; y <= 66; y++)
                for (var x = 46; x <= 50; x++)
                    photo[y * W + x] = bg[y * W + x];

            var r = BackgroundSubtraction.Cut(bg, photo, W, H);

            Assert.IsTrue(r.Ok, r.Reason);
            var area = Mathf.PI * radius * radius;
            Assert.That(OpaqueCount(r), Is.GreaterThan(area * 0.92f));
        }

        [Test]
        public void 小さなごみは消える()
        {
            var bg = DirtyBackground();
            var radius = 0.17f * W;
            var photo = WithDoll(bg, 0.5f * W, 0.5f * H, radius);
            // 枠の中の離れた所に、小さなごみ（3 x 3）
            for (var y = 20; y <= 22; y++)
                for (var x = 46; x <= 48; x++)
                    photo[y * W + x] = new Color32(250, 250, 20, 255);

            var r = BackgroundSubtraction.Cut(bg, photo, W, H);

            Assert.IsTrue(r.Ok, r.Reason);
            // ごみが残ると、切り抜きの縦が、大きく伸びる
            Assert.That(r.Height, Is.LessThan(2 * radius + 10));
        }

        [Test]
        public void 大きさが合わない写真は失敗する()
        {
            Assert.IsFalse(BackgroundSubtraction.Cut(new Color32[4], new Color32[4], W, H).Ok);
            Assert.IsFalse(BackgroundSubtraction.Cut(null, null, W, H).Ok);
        }
    }
}
