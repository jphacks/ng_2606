using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    /// <summary>HumanMask：キャラを撮るときの、顔だけのまるい枠。</summary>
    public class HumanMaskTests
    {
        [Test]
        public void 顔の真ん中は枠の中()
        {
            Assert.AreEqual(1f, HumanMask.Coverage(0.50f, 0.50f), 1e-4f);
            Assert.AreEqual(1f, HumanMask.Coverage(0.50f, 0.80f), 1e-4f);
        }

        [Test]
        public void 箱の四隅は枠の外()
        {
            Assert.AreEqual(0f, HumanMask.Coverage(0.01f, 0.01f));
            Assert.AreEqual(0f, HumanMask.Coverage(0.99f, 0.01f));
            Assert.AreEqual(0f, HumanMask.Coverage(0.01f, 0.99f));
            Assert.AreEqual(0f, HumanMask.Coverage(0.99f, 0.99f));
        }

        [Test]
        public void 顔は少し縦に長い()
        {
            // 写真の中で、縦の長さ（w × 高さ）が、横の長さ（z × 幅）より長い
            var f = HumanMask.Face;
            Assert.Greater(f.w, f.z * HumanMask.Aspect);
        }

        [Test]
        public void 枠は太め()
        {
            // 箱の面積の、半分より少し多いくらい
            var area = HumanMask.AreaFraction();
            Assert.That(area, Is.InRange(0.45f, 0.75f), $"面積の割合 {area:0.00}");
        }

        [Test]
        public void 写真に枠の形の透明度が付く()
        {
            var w = 30;
            var h = 40;
            var rgb = new Color32[w * h];
            for (var i = 0; i < rgb.Length; i++) rgb[i] = new Color32(10, 20, 30, 255);

            var masked = HumanMask.Apply(rgb, w, h);

            // 真ん中（顔）は不透明、左下の隅は透明。色は変わらない
            var center = masked[(h / 2) * w + w / 2];
            Assert.AreEqual(255, center.a);
            Assert.AreEqual(10, center.r);
            Assert.AreEqual(0, masked[0].a);
        }

        [Test]
        public void 映像の中の枠の場所は中央で収まる()
        {
            var wide = HumanMask.CropRegion(4f / 3f);
            Assert.AreEqual(0.5f, wide.center.x, 1e-4f);
            Assert.AreEqual(0.5f, wide.center.y, 1e-4f);
            // 枠の箱の縦横の比（3:4）が、映像の画素の上でも保たれる
            Assert.AreEqual(HumanMask.Aspect, wide.width * (4f / 3f) / wide.height, 1e-3f);
            Assert.LessOrEqual(wide.width, 0.92f + 1e-4f);

            // 縦長の映像でも、横に収まる
            var tall = HumanMask.CropRegion(0.5f);
            Assert.LessOrEqual(tall.width, 0.92f + 1e-4f);
            Assert.AreEqual(HumanMask.Aspect, tall.width * 0.5f / tall.height, 1e-3f);
        }

        [Test]
        public void 案内の絵は枠の中が透明で外が暗い()
        {
            var w = 80;
            var h = 60;
            var region = HumanMask.CropRegion((float)w / h);
            var overlay = HumanMask.GuideOverlay(w, h, region);

            // 映像の中央（胴）は透明
            Assert.AreEqual(0, overlay[(h / 2) * w + w / 2].a);
            // 映像の左下の隅（枠の外）は暗い
            Assert.Greater(overlay[0].a, 100);
        }
    }
}
