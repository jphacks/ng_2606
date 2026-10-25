using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    /// <summary>HumanMask：人形を撮るときの、太めで 3 等身くらいの人型の枠。</summary>
    public class HumanMaskTests
    {
        [Test]
        public void 頭と胴と足の中心は枠の中()
        {
            Assert.AreEqual(1f, HumanMask.Coverage(0.50f, 0.80f), 1e-4f); // 頭
            Assert.AreEqual(1f, HumanMask.Coverage(0.50f, 0.46f), 1e-4f); // 胴
            Assert.AreEqual(1f, HumanMask.Coverage(0.36f, 0.13f), 1e-4f); // 左あし
            Assert.AreEqual(1f, HumanMask.Coverage(0.64f, 0.13f), 1e-4f); // 右あし
            Assert.AreEqual(1f, HumanMask.Coverage(0.14f, 0.50f), 1e-4f); // 左うで
            Assert.AreEqual(1f, HumanMask.Coverage(0.86f, 0.50f), 1e-4f); // 右うで
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
        public void 頭の横と足のあいだは外から区別できる()
        {
            // 頭の左右、肩の上は、枠の外（人型のくびれ）
            Assert.AreEqual(0f, HumanMask.Coverage(0.10f, 0.85f));
            Assert.AreEqual(0f, HumanMask.Coverage(0.90f, 0.85f));
        }

        [Test]
        public void 頭の高さは全体の約3分の1()
        {
            // 頭の上端（0.99 付近）は枠の中、頭の高さは 0.38。全体の高さ 1 に対して、約 2.6 頭身（3 等身に近い）
            Assert.Greater(HumanMask.Coverage(0.50f, 0.98f), 0f);
            Assert.AreEqual(0f, HumanMask.Coverage(0.50f, 1.0f));
            var headHeight = 2f * 0.19f;
            Assert.That(1f / headHeight, Is.InRange(2.4f, 3.2f));
        }

        [Test]
        public void 枠は太め()
        {
            // 箱の面積の、半分より少し多いくらい（細すぎると、いろいろなキャラが入らない）
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

            // 真ん中（胴）は不透明、左下の隅は透明。色は変わらない
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
