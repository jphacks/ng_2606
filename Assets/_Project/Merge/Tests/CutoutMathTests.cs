using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    /// <summary>CutoutMath：人形の写真から人形の部分だけを切り抜く計算。</summary>
    public class CutoutMathTests
    {
        static readonly Color32 White = new Color32(240, 240, 240, 255);
        static readonly Color32 Red = new Color32(200, 30, 30, 255);
        static readonly Color32 Blue = new Color32(30, 30, 200, 255);

        /// <summary>背景色 bg の画像に、(x0,y0)〜(x1,y1)（含む）を fg で塗る。</summary>
        static Color32[] Image(int w, int h, Color32 bg, Color32 fg, int x0, int y0, int x1, int y1)
        {
            var px = new Color32[w * h];
            for (var i = 0; i < px.Length; i++) px[i] = bg;
            for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    px[y * w + x] = fg;
            return px;
        }

        [Test]
        public void 白い背景の赤い四角が切り抜かれる()
        {
            var src = Image(100, 100, White, Red, 30, 30, 69, 79);
            var r = CutoutMath.Cut(src, 100, 100);

            Assert.IsTrue(r.Ok, r.Reason);
            // 40 x 50 の四角に、余白 2 画素ずつ
            Assert.AreEqual(44, r.Width);
            Assert.AreEqual(54, r.Height);
            // 真ん中は不透明で赤、角は透明
            var center = r.Pixels[(r.Height / 2) * r.Width + r.Width / 2];
            Assert.AreEqual(255, center.a);
            Assert.AreEqual(200, center.r);
            Assert.AreEqual(0, r.Pixels[0].a);
        }

        [Test]
        public void 人形の中の背景と同じ色の穴は残る()
        {
            var src = Image(100, 100, White, Red, 20, 20, 79, 79);
            // 中に白い穴（縁につながっていない）
            for (var y = 45; y <= 55; y++)
                for (var x = 45; x <= 55; x++)
                    src[y * 100 + x] = White;
            var r = CutoutMath.Cut(src, 100, 100);

            Assert.IsTrue(r.Ok, r.Reason);
            // 穴の中心（切り抜きの座標：元の (50, 50) − 左下 (20 − 2, 20 − 2)）は、不透明のまま
            var hole = r.Pixels[(50 - 18) * r.Width + (50 - 18)];
            Assert.AreEqual(255, hole.a);
        }

        [Test]
        public void 縁と違う色の背景でも縁の色を背景にする()
        {
            var src = Image(100, 100, Blue, Red, 30, 30, 69, 69);
            var r = CutoutMath.Cut(src, 100, 100);

            Assert.IsTrue(r.Ok, r.Reason);
            Assert.AreEqual(44, r.Width);
        }

        [Test]
        public void 小さなごみは消える()
        {
            var src = Image(100, 100, White, Red, 30, 30, 69, 69);
            // 離れた所の小さなごみ（3 x 3）
            for (var y = 5; y <= 7; y++)
                for (var x = 5; x <= 7; x++)
                    src[y * 100 + x] = Red;
            var r = CutoutMath.Cut(src, 100, 100);

            Assert.IsTrue(r.Ok, r.Reason);
            // ごみが残っていれば、切り抜きが広がってしまう
            Assert.AreEqual(44, r.Width);
            Assert.AreEqual(44, r.Height);
        }

        [Test]
        public void 縁がすべて同じ色ならその色が背景になる()
        {
            // 縁がすべて赤。内側に白い塊
            var src = new Color32[100 * 100];
            for (var i = 0; i < src.Length; i++) src[i] = Red;
            // 縁の中で、縁の色とは違う大きな塊だけが白
            for (var y = 20; y < 80; y++)
                for (var x = 20; x < 80; x++)
                    src[y * 100 + x] = White;
            var r = CutoutMath.Cut(src, 100, 100);

            // 縁の色（赤）を背景とみなす：縁から赤が広がり、背景が見つかる。内側の白が人形になる（背景の色は、縁で決まる）
            Assert.IsTrue(r.Ok, r.Reason);
            Assert.AreEqual(64, r.Width);
        }

        [Test]
        public void 縁の色がばらばらだと失敗する()
        {
            var rnd = new System.Random(1);
            var src = new Color32[100 * 100];
            for (var i = 0; i < src.Length; i++)
            {
                src[i] = new Color32((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256), 255);
            }
            var r = CutoutMath.Cut(src, 100, 100);

            Assert.IsFalse(r.Ok);
            Assert.IsNotEmpty(r.Reason);
        }

        [Test]
        public void 人形が見つからないと失敗する()
        {
            var src = Image(100, 100, White, White, 0, 0, 0, 0);
            var r = CutoutMath.Cut(src, 100, 100);

            Assert.IsFalse(r.Ok);
            Assert.IsNotEmpty(r.Reason);
        }

        [Test]
        public void 小さすぎる写真は失敗する()
        {
            Assert.IsFalse(CutoutMath.Cut(new Color32[4], 2, 2).Ok);
            Assert.IsFalse(CutoutMath.Cut(null, 100, 100).Ok);
        }
    }
}
