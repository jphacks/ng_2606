using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>PhotoTextureMath：写真から箱の上面や机の絵を切り出す計算。</summary>
    public class PhotoTextureMathTests
    {
        const float Tol = 1e-4f;

        static LlmQuad Quad(params float[] v) => new LlmQuad
        {
            x0 = v[0], y0 = v[1], x1 = v[2], y1 = v[3], x2 = v[4], y2 = v[5], x3 = v[6], y3 = v[7],
        };

        // ---- ValidQuad ----

        [Test]
        public void 普通の四隅は使える()
        {
            var q = PhotoTextureMath.ValidQuad(Quad(0.2f, 0.2f, 0.6f, 0.2f, 0.6f, 0.5f, 0.2f, 0.5f));
            Assert.IsNotNull(q);
            Assert.AreEqual(4, q.Length);
        }

        [Test]
        public void 八つとも0なら分からないとして使わない()
        {
            Assert.IsNull(PhotoTextureMath.ValidQuad(Quad(0, 0, 0, 0, 0, 0, 0, 0)));
            Assert.IsNull(PhotoTextureMath.ValidQuad(null));
        }

        [Test]
        public void 範囲の外や面積がほぼ0の四隅は使わない()
        {
            Assert.IsNull(PhotoTextureMath.ValidQuad(Quad(0.2f, 0.2f, 1.5f, 0.2f, 0.6f, 0.5f, 0.2f, 0.5f)));
            // 一直線
            Assert.IsNull(PhotoTextureMath.ValidQuad(Quad(0.1f, 0.1f, 0.2f, 0.2f, 0.3f, 0.3f, 0.4f, 0.4f)));
        }

        [Test]
        public void ほんの少し外に出た四隅は0から1に収める()
        {
            var q = PhotoTextureMath.ValidQuad(Quad(-0.0005f, 0.2f, 0.6f, 0.2f, 0.6f, 0.5f, 1.0005f, 0.5f));
            Assert.IsNotNull(q);
            Assert.AreEqual(0f, q[0].x, Tol);
            Assert.AreEqual(1f, q[3].x, Tol);
        }

        // ---- OrderForFace ----

        // 写真の左上から時計回り：左上、右上、右下、左下
        static readonly Vector2[] Clockwise =
        {
            new Vector2(0.2f, 0.2f), new Vector2(0.6f, 0.2f), new Vector2(0.6f, 0.5f), new Vector2(0.2f, 0.5f),
        };

        [Test]
        public void 回していない面は写真の左上が左奥になる()
        {
            var r = PhotoTextureMath.OrderForFace(Clockwise, 0.4f, 0.3f, 0f);
            Assert.AreEqual(Clockwise[0], r[PhotoTextureMath.FarLeft]);
            Assert.AreEqual(Clockwise[1], r[PhotoTextureMath.FarRight]);
            Assert.AreEqual(Clockwise[2], r[PhotoTextureMath.NearRight]);
            Assert.AreEqual(Clockwise[3], r[PhotoTextureMath.NearLeft]);
        }

        [Test]
        public void どの角から始めても同じ並びになる()
        {
            var shifted = new[] { Clockwise[2], Clockwise[3], Clockwise[0], Clockwise[1] };
            var r = PhotoTextureMath.OrderForFace(shifted, 0.4f, 0.3f, 0f);
            Assert.AreEqual(Clockwise[0], r[PhotoTextureMath.FarLeft]);
            Assert.AreEqual(Clockwise[2], r[PhotoTextureMath.NearRight]);
        }

        [Test]
        public void 反時計回りの答えでも同じ並びになる()
        {
            var counter = new[] { Clockwise[0], Clockwise[3], Clockwise[2], Clockwise[1] };
            var r = PhotoTextureMath.OrderForFace(counter, 0.4f, 0.3f, 0f);
            Assert.AreEqual(Clockwise[0], r[PhotoTextureMath.FarLeft]);
            Assert.AreEqual(Clockwise[1], r[PhotoTextureMath.FarRight]);
            Assert.AreEqual(Clockwise[2], r[PhotoTextureMath.NearRight]);
            Assert.AreEqual(Clockwise[3], r[PhotoTextureMath.NearLeft]);
        }

        [Test]
        public void 九十度回した面は角が一つずれる()
        {
            // Unity の Y 軸まわり 90 度（上から見て時計回り）：面の右（+X）が、ステージの手前（−Z）を向く。
            // 写真では、面の左奥（−X, +Z）が、写真の右上へ動く
            var r = PhotoTextureMath.OrderForFace(Clockwise, 0.3f, 0.3f, 90f);
            Assert.AreEqual(Clockwise[1], r[PhotoTextureMath.FarLeft]);
            Assert.AreEqual(Clockwise[2], r[PhotoTextureMath.FarRight]);
            Assert.AreEqual(Clockwise[3], r[PhotoTextureMath.NearRight]);
            Assert.AreEqual(Clockwise[0], r[PhotoTextureMath.NearLeft]);
        }

        // ---- Crop ----

        /// <summary>横に赤が 0→255、縦（写真の上から）に緑が 0→255 と変わる写真。</summary>
        static Color32[] Gradient(int w, int h)
        {
            var px = new Color32[w * h];
            for (var row = 0; row < h; row++) // 配列の row は下から
            {
                var yTop = h - 1 - row;
                for (var x = 0; x < w; x++)
                {
                    px[row * w + x] = new Color32((byte)(x * 255 / (w - 1)), (byte)(yTop * 255 / (h - 1)), 0, 255);
                }
            }
            return px;
        }

        [Test]
        public void 写真全体を切り出すと向きが保たれる()
        {
            var src = Gradient(64, 64);
            var all = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            var crop = PhotoTextureMath.Crop(src, 64, 64, all, 8, 8);

            // 添字 0 は左下の行。左下は、赤が小さく、緑（上から）が大きい
            var leftBottom = crop[0];
            var rightTop = crop[7 * 8 + 7];
            Assert.Less(leftBottom.r, 40);
            Assert.Greater(leftBottom.g, 215);
            Assert.Greater(rightTop.r, 215);
            Assert.Less(rightTop.g, 40);
        }

        [Test]
        public void 一部を切り出すとその部分の色になる()
        {
            var src = Gradient(100, 100);
            // 写真の右半分の、上半分（x 0.5〜1、y 0〜0.5）
            var quad = new[] { new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f) };
            var crop = PhotoTextureMath.Crop(src, 100, 100, quad, 4, 4);
            foreach (var c in crop)
            {
                Assert.GreaterOrEqual(c.r, 120); // 右半分
                Assert.LessOrEqual(c.g, 135);    // 上半分
            }
        }

        [Test]
        public void 回した四隅は回して切り出される()
        {
            var src = Gradient(100, 100);
            // 左奥と右奥を入れ替える（左右反転）：切り出しの左が、写真の右になる
            var flipped = new[] { new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            var crop = PhotoTextureMath.Crop(src, 100, 100, flipped, 8, 8);
            Assert.Greater(crop[0].r, 200);       // 左下の画素は、写真の右側（赤が大きい）
            Assert.Less(crop[7].r, 55);           // 右下の画素は、写真の左側
        }

        [Test]
        public void 範囲を外へ広げると机の外側ものびて切り出される()
        {
            var src = Gradient(100, 100);
            // 机は写真の中央（x 0.25〜0.75、y 0.25〜0.75）
            var desk = new[] { new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f), new Vector2(0.75f, 0.75f), new Vector2(0.25f, 0.75f) };
            // u、v を -0.5〜1.5 にすると、写真の全体（0〜1）になる
            var crop = PhotoTextureMath.CropRange(src, 100, 100, desk, 8, 8, -0.5f, 1.5f, -0.5f, 1.5f);

            // 左下の画素は、写真の左下（赤が小さく、緑が大きい）
            Assert.Less(crop[0].r, 40);
            Assert.Greater(crop[0].g, 215);
            // 右上の画素は、写真の右上
            Assert.Greater(crop[7 * 8 + 7].r, 215);
            Assert.Less(crop[7 * 8 + 7].g, 40);
        }

        [Test]
        public void 写真の外へのびた部分はいちばん外の色で埋まる()
        {
            var src = Gradient(100, 100);
            var all = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            // 写真の左へ大きくはみ出す
            var crop = PhotoTextureMath.CropRange(src, 100, 100, all, 8, 8, -3f, 1f, 0f, 1f);
            // いちばん左の画素は、写真の左端の色（赤がほぼ 0）
            Assert.Less(crop[0].r, 5);
        }

        [Test]
        public void 物の場所は左右の外側の色でつながってぬりつぶされる()
        {
            // 横に黒 → 白と変わる写真に、真ん中に赤い四角（物）
            var w = 100;
            var h = 50;
            var src = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var v = (byte)(x * 255 / (w - 1));
                    src[y * w + x] = new Color32(v, v, v, 255);
                }
            for (var y = 20; y < 30; y++)
                for (var x = 40; x < 60; x++)
                    src[y * w + x] = new Color32(255, 0, 0, 255);

            // 物の場所：x 0.4〜0.6、y（上から）0.4〜0.6
            var filled = PhotoTextureMath.FillRegions(src, w, h, new[] { Rect.MinMaxRect(0.4f, 0.4f, 0.6f, 0.6f) });

            // 赤は消え、左右の灰色をつないだ色になる（左端はほぼ 101、右端はほぼ 153）
            var inside = filled[(h - 1 - 25) * w + 50];
            Assert.AreEqual(inside.r, inside.g, 3);
            Assert.AreEqual(inside.g, inside.b, 3);
            Assert.That(inside.r, Is.InRange(110, 145));
            // 場所の外は、そのまま
            Assert.AreEqual(src[0].r, filled[0].r);
            Assert.AreEqual(src[(h - 1 - 5) * w + 80].r, filled[(h - 1 - 5) * w + 80].r);
        }

        [Test]
        public void ぬりつぶしは元の写真を変えない()
        {
            var src = new Color32[10 * 10];
            for (var i = 0; i < src.Length; i++) src[i] = new Color32(10, 20, 30, 255);
            src[55] = new Color32(200, 0, 0, 255);

            PhotoTextureMath.FillRegions(src, 10, 10, new[] { Rect.MinMaxRect(0.4f, 0.4f, 0.7f, 0.7f) });

            Assert.AreEqual(200, src[55].r);
        }

        [Test]
        public void 写真の端の物は片側の色でぬりつぶされる()
        {
            var w = 20;
            var h = 10;
            var src = new Color32[w * h];
            for (var i = 0; i < src.Length; i++) src[i] = new Color32(50, 60, 70, 255);
            // 右端の物
            for (var y = 0; y < h; y++)
                for (var x = 15; x < w; x++)
                    src[y * w + x] = new Color32(255, 0, 0, 255);

            var filled = PhotoTextureMath.FillRegions(src, w, h, new[] { Rect.MinMaxRect(0.75f, 0f, 1f, 1f) });

            Assert.AreEqual(50, filled[5 * w + 18].r);
            Assert.AreEqual(60, filled[5 * w + 18].g);
        }

        [Test]
        public void 平均の色()
        {
            var avg = PhotoTextureMath.Average(new[] { new Color32(0, 100, 200, 255), new Color32(100, 200, 100, 255) });
            Assert.AreEqual(50, avg.r);
            Assert.AreEqual(150, avg.g);
            Assert.AreEqual(150, avg.b);
        }
    }
}
