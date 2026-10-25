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
    }
}
