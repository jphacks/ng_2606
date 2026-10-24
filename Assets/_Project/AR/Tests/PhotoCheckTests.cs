using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.AR.Tests
{
    /// <summary>PhotoCheck：写真の暗さとぶれ（A の Step 23）。</summary>
    public class PhotoCheckTests
    {
        const float MinBrightness = 0.12f;
        const float MinSharpness = 0.0003f;

        static Color32[] Fill(int w, int h, System.Func<int, int, byte> value)
        {
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var v = value(x, y);
                    pixels[y * w + x] = new Color32(v, v, v, 255);
                }
            }
            return pixels;
        }

        /// <summary>白と灰色の細かい市松模様（くっきりした写真の代わり）。</summary>
        static Color32[] Checker(int w, int h, byte light, byte dark)
        {
            return Fill(w, h, (x, y) => ((x / 2 + y / 2) % 2 == 0) ? light : dark);
        }

        [Test]
        public void 真っ黒の写真は暗い()
        {
            var q = PhotoCheck.Measure(Fill(200, 150, (x, y) => 0), 200, 150);

            Assert.That(q.brightness, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(PhotoCheck.Judge(q, MinBrightness, MinSharpness), Is.EqualTo(ScanReason.Dark));
        }

        [Test]
        public void くっきりした明るい写真は通る()
        {
            var q = PhotoCheck.Measure(Checker(400, 300, 230, 120), 400, 300);

            Assert.That(q.brightness, Is.GreaterThan(MinBrightness));
            Assert.That(PhotoCheck.Judge(q, MinBrightness, MinSharpness), Is.EqualTo(ScanReason.None));
        }

        [Test]
        public void なだらかで模様のない写真はぶれとみなす()
        {
            // 左から右へゆっくり明るくなるだけの写真（ぼけた写真の代わり）
            var q = PhotoCheck.Measure(Fill(400, 300, (x, y) => (byte)(100 + x / 4)), 400, 300);

            Assert.That(q.brightness, Is.GreaterThan(MinBrightness));
            Assert.That(PhotoCheck.Judge(q, MinBrightness, MinSharpness), Is.EqualTo(ScanReason.FastMotion));
        }

        [Test]
        public void 大きな写真でも間引いて測れる()
        {
            var q = PhotoCheck.Measure(Checker(1568, 1176, 230, 120), 1568, 1176);

            Assert.That(PhotoCheck.Judge(q, MinBrightness, MinSharpness), Is.EqualTo(ScanReason.None));
        }

        [Test]
        public void 暗さはぶれより先に判定する()
        {
            var q = new PhotoQuality { brightness = 0.05f, sharpness = 0f };

            Assert.That(PhotoCheck.Judge(q, MinBrightness, MinSharpness), Is.EqualTo(ScanReason.Dark));
        }
    }
}
