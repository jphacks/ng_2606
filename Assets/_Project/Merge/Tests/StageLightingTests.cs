using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    /// <summary>遊ぶステージの光（StageLighting）の色の決め方。</summary>
    public class StageLightingTests
    {
        // 上半分が青、下半分が茶色の背景
        static Color32[] SkyAndFloor(int w, int h)
        {
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    pixels[y * w + x] = y >= h / 2 ? new Color32(80, 150, 255, 255) : new Color32(140, 100, 60, 255);
                }
            }
            return pixels;
        }

        [Test]
        public void 背景が無いときは決まった色()
        {
            StageLighting.AmbientColors(null, 0, 0, out var sky, out var equator, out var ground);
            Assert.Greater(sky.b, ground.b);
            Assert.AreEqual(1f, sky.a);
        }

        [Test]
        public void 空は背景の上の色に地面は下の色に寄る()
        {
            var pixels = SkyAndFloor(8, 8);
            StageLighting.AmbientColors(pixels, 8, 8, out var sky, out _, out var ground);
            Assert.Greater(sky.b, sky.r, "空は青っぽい");
            Assert.Greater(ground.r, ground.b, "地面は茶色っぽい");
            Assert.Less(ground.grayscale, sky.grayscale, "地面は空より暗い");
        }

        [Test]
        public void 環境の光は背景そのものより弱い()
        {
            var pixels = SkyAndFloor(8, 8);
            StageLighting.AmbientColors(pixels, 8, 8, out var sky, out _, out _);
            Assert.Less(sky.grayscale, new Color(80 / 255f, 150 / 255f, 1f).grayscale);
        }

        [Test]
        public void 光の色は白に近く背景の色に少しだけ寄る()
        {
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 120, 40, 255); // ゆうやけ
            var color = StageLighting.LightColor(pixels, 4, 4);
            Assert.Greater(color.r, color.b, "少しオレンジ");
            Assert.Greater(color.b, 0.7f, "白から離れすぎない");
            Assert.AreEqual(StageLighting.LightColor(null, 0, 0).r, 1f, 1e-4f);
        }
    }
}
