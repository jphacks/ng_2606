using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    public class GimmickDetectorTests
    {
        static Color32[] Fill(int count, Color32 color)
        {
            var pixels = new Color32[count];
            for (var i = 0; i < count; i++) pixels[i] = color;
            return pixels;
        }

        // 一部だけ、別の色にした列
        static Color32[] Mixed(int count, Color32 main, Color32 other, float otherRatio)
        {
            var pixels = Fill(count, main);
            var n = Mathf.RoundToInt(count * otherRatio);
            for (var i = 0; i < n; i++) pixels[i] = other;
            return pixels;
        }

        static readonly Color32 Blue = new Color32(30, 90, 220, 255);
        static readonly Color32 Red = new Color32(220, 30, 40, 255);
        static readonly Color32 Green = new Color32(30, 170, 70, 255);
        static readonly Color32 Cardboard = new Color32(200, 150, 100, 255);
        static readonly Color32 White = new Color32(240, 240, 240, 255);

        [Test]
        public void 一面が青なら水()
        {
            Assert.AreEqual(BoxData.GimmickWater, GimmickDetector.Detect(Fill(400, Blue)));
        }

        [Test]
        public void 一面が赤なら炎()
        {
            Assert.AreEqual(BoxData.GimmickFire, GimmickDetector.Detect(Fill(400, Red)));
        }

        [Test]
        public void 一面が緑なら回復()
        {
            Assert.AreEqual(BoxData.GimmickHeal, GimmickDetector.Detect(Fill(400, Green)));
        }

        [Test]
        public void 少し青が入っているだけではしかけにならない()
        {
            Assert.AreEqual("", GimmickDetector.Detect(Mixed(400, Cardboard, Blue, 0.3f)));
            Assert.AreEqual("", GimmickDetector.Detect(Mixed(400, White, Blue, 0.5f)));
        }

        [Test]
        public void ほとんどが青ならしかけになる()
        {
            Assert.AreEqual(BoxData.GimmickWater, GimmickDetector.Detect(Mixed(400, Blue, White, 0.1f)));
        }

        [Test]
        public void 割合の境目()
        {
            // 15% が別の色（ちょうど境目）は水、20% が別の色なら、ならない
            Assert.AreEqual(BoxData.GimmickWater, GimmickDetector.Detect(Mixed(400, Blue, White, 0.15f)));
            Assert.AreEqual("", GimmickDetector.Detect(Mixed(400, Blue, White, 0.20f)));
        }

        [Test]
        public void 段ボールと白と黒と灰色は何にもならない()
        {
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, Cardboard)));
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, White)));
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, new Color32(10, 10, 10, 255))));
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, new Color32(128, 128, 128, 255))));
        }

        [Test]
        public void 暗い青も明るい青も水()
        {
            Assert.AreEqual(BoxData.GimmickWater, GimmickDetector.Detect(Fill(400, new Color32(20, 50, 140, 255))));
            Assert.AreEqual(BoxData.GimmickWater, GimmickDetector.Detect(Fill(400, new Color32(110, 170, 250, 255))));
        }

        [Test]
        public void 画素が無ければ空()
        {
            Assert.AreEqual("", GimmickDetector.Detect(null));
            Assert.AreEqual("", GimmickDetector.Detect(new Color32[0]));
        }

        [Test]
        public void 実機で撮った折り紙の色が判定される()
        {
            // 実機の写真から測った値：緑は青緑（色相 178、暗め）、赤は薄い所も暗い所もある、青は色相 214
            Assert.AreEqual(BoxData.GimmickHeal, GimmickDetector.Detect(Fill(400, new Color32(0, 138, 134, 255))));
            Assert.AreEqual(BoxData.GimmickHeal, GimmickDetector.Detect(Fill(400, new Color32(0, 105, 100, 255))));
            Assert.AreEqual(BoxData.GimmickFire, GimmickDetector.Detect(Fill(400, new Color32(232, 98, 105, 255))));
            Assert.AreEqual(BoxData.GimmickFire, GimmickDetector.Detect(Fill(400, new Color32(135, 63, 67, 255))));
            Assert.AreEqual(BoxData.GimmickWater, GimmickDetector.Detect(Fill(400, new Color32(16, 121, 222, 255))));
        }

        [Test]
        public void 黄緑も緑になり茶色のテープは何にもならない()
        {
            Assert.AreEqual(BoxData.GimmickHeal, GimmickDetector.Detect(Fill(400, new Color32(120, 190, 50, 255))));
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, new Color32(190, 140, 110, 255)))); // 茶色のテープ
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, new Color32(150, 125, 95, 255)))); // 段ボール
        }

        [Test]
        public void オレンジは赤にならない()
        {
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, new Color32(240, 130, 30, 255))));
        }

        // ---- 2026-11-06 に足した色（実機ではまだ測っていない） ----

        [Test]
        public void 一面の黄色はジャンプ台()
        {
            Assert.AreEqual(BoxData.GimmickJumpPad, GimmickDetector.Detect(Fill(400, new Color32(240, 200, 30, 255))));
            Assert.AreEqual(BoxData.GimmickJumpPad, GimmickDetector.Detect(Fill(400, new Color32(250, 225, 60, 255))));
        }

        [Test]
        public void 一面の紫は敵()
        {
            Assert.AreEqual(BoxData.GimmickEnemy, GimmickDetector.Detect(Fill(400, new Color32(130, 60, 190, 255))));
            Assert.AreEqual(BoxData.GimmickEnemy, GimmickDetector.Detect(Fill(400, new Color32(90, 50, 150, 255))));
        }

        [Test]
        public void 一面のピンクはワープ()
        {
            Assert.AreEqual(BoxData.GimmickWarp, GimmickDetector.Detect(Fill(400, new Color32(245, 150, 190, 255))));
            Assert.AreEqual(BoxData.GimmickWarp, GimmickDetector.Detect(Fill(400, new Color32(250, 180, 190, 255))));
            Assert.AreEqual(BoxData.GimmickWarp, GimmickDetector.Detect(Fill(400, new Color32(230, 80, 170, 255))));
        }

        [Test]
        public void あざやかな赤は赤のままで淡い赤はピンク()
        {
            Assert.AreEqual(BoxData.GimmickFire, GimmickDetector.Detect(Fill(400, Red)));
            Assert.AreEqual(BoxData.GimmickWarp, GimmickDetector.Detect(Fill(400, new Color32(250, 175, 180, 255))));
        }

        [Test]
        public void 新しい色も85パーセント以上のときだけ()
        {
            var yellow = new Color32(240, 200, 30, 255);
            Assert.AreEqual(BoxData.GimmickJumpPad, GimmickDetector.Detect(Mixed(400, yellow, White, 0.15f)));
            Assert.AreEqual("", GimmickDetector.Detect(Mixed(400, yellow, White, 0.20f)));
        }

        [Test]
        public void 段ボールとテープとオレンジは黄色にならない()
        {
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, Cardboard)));
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, new Color32(150, 125, 95, 255))));
            Assert.AreEqual("", GimmickDetector.Detect(Fill(400, new Color32(240, 130, 30, 255))));
        }

        [Test]
        public void 実機で撮った黄と紫の折り紙が判定され_木の床は何にもならない()
        {
            // 2026-11-07 に実機で測った色（黄：色相 62 度・彩度 0.46、紫：色相 248 度・彩度 0.55、床：色相 45〜51 度・彩度 0.37）
            Assert.AreEqual(GimmickDetector.Hue.Yellow, GimmickDetector.Classify(new Color32(216, 219, 118, 255)));
            Assert.AreEqual(GimmickDetector.Hue.Purple, GimmickDetector.Classify(new Color32(84, 72, 161, 255)));
            Assert.AreEqual(GimmickDetector.Hue.None, GimmickDetector.Classify(new Color32(214, 194, 135, 255)));
            Assert.AreEqual(GimmickDetector.Hue.None, GimmickDetector.Classify(new Color32(204, 193, 131, 255)));
            Assert.AreEqual(GimmickDetector.Hue.Blue, GimmickDetector.Classify(new Color32(30, 90, 220, 255)), "実機の青（213〜215 度）は青のまま");
        }

        [Test]
        public void 実機のピンクの折り紙の端の色も_紫ではなくピンク()
        {
            // 2026-11-07 に実機で測った色（ピンク：色相 299 度・彩度 0.43、紫：色相 248 度・彩度 0.54）
            Assert.AreEqual(GimmickDetector.Hue.Pink, GimmickDetector.Classify(new Color32(235, 135, 237, 255)));
            Assert.AreEqual(GimmickDetector.Hue.Purple, GimmickDetector.Classify(new Color32(91, 79, 171, 255)));
        }

        [Test]
        public void 実機の濃いピンクの紙は赤ではなくピンク_赤い紙と箱は赤のまま()
        {
            // 2026-11-14 に実機で測った色（濃いピンクの紙：色相 340 度・彩度 0.59、淡いピンクの箱：333 度・0.54、
            // 赤い箱：8 度・0.92、赤の折り紙：357 度・0.55）
            Assert.AreEqual(GimmickDetector.Hue.Pink, GimmickDetector.Classify(new Color32(184, 75, 111, 255)), "濃いピンクの紙");
            Assert.AreEqual(GimmickDetector.Hue.Pink, GimmickDetector.Classify(new Color32(242, 111, 170, 255)), "淡いピンクの箱");
            Assert.AreEqual(GimmickDetector.Hue.Red, GimmickDetector.Classify(new Color32(163, 33, 13, 255)), "赤い箱");
            Assert.AreEqual(GimmickDetector.Hue.Red, GimmickDetector.Classify(new Color32(204, 92, 97, 255)), "赤の折り紙");
            Assert.AreEqual(BoxData.GimmickWarp, GimmickDetector.Detect(Fill(400, new Color32(184, 75, 111, 255))));
        }

        [Test]
        public void 暖色の光で赤寄りになった暗い紫は紫_明るいピンクはピンクのまま()
        {
            // 暖色の光の紫（色相 300〜315 度・明るさ 0.44〜0.5）。ピンクは明るい（実機で 0.72〜0.96）
            Assert.AreEqual(GimmickDetector.Hue.Purple, GimmickDetector.Classify(new Color32(112, 65, 112, 255)), "暖色の紫");
            Assert.AreEqual(GimmickDetector.Hue.Purple, GimmickDetector.Classify(new Color32(128, 70, 113, 255)), "暖色の紫（もっと赤寄り）");
            Assert.AreEqual(GimmickDetector.Hue.Pink, GimmickDetector.Classify(new Color32(184, 75, 111, 255)), "濃いピンクの紙");
            Assert.AreEqual(GimmickDetector.Hue.Pink, GimmickDetector.Classify(new Color32(235, 135, 237, 255)), "ピンクの折り紙");
            Assert.AreEqual(GimmickDetector.Hue.Pink, GimmickDetector.Classify(new Color32(158, 79, 132, 255)), "少し影になったピンク");
            Assert.AreEqual(BoxData.GimmickEnemy, GimmickDetector.Detect(Fill(400, new Color32(112, 65, 112, 255))));
        }
    }
}
