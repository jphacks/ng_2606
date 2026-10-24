using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>写真の明るさとくっきりさ。</summary>
    public struct PhotoQuality
    {
        /// <summary>平均の明るさ（0＝真っ黒 〜 1＝真っ白）。</summary>
        public float brightness;

        /// <summary>くっきりさ（明るさの変化の大きさ。ラプラシアンの分散）。ぶれやぼけで小さくなる。</summary>
        public float sharpness;
    }

    /// <summary>
    /// 送る前に、写真が暗すぎないか、ぶれていないかを確かめる（A の Step 23）。
    /// 使えない写真を送ると、時間と API の料金がむだになるため。
    /// しきい値の仮の値は、試しの写真を暗くしたりぼかしたりして決めた（Docs/LlmScanFormat.md と同じ写真）。
    /// </summary>
    public static class PhotoCheck
    {
        /// <summary>長い辺をこのくらいの点の数に間引いて調べる（写真の大きさで値が変わらないように）。</summary>
        const int SampleLongSide = 512;

        public static PhotoQuality Measure(Color32[] pixels, int width, int height)
        {
            var step = Mathf.Max(1, Mathf.Max(width, height) / SampleLongSide);
            var w = (width + step - 1) / step;
            var h = (height + step - 1) / step;
            var gray = new float[w * h];
            double sum = 0;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var c = pixels[(y * step) * width + x * step];
                    var g = (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
                    gray[y * w + x] = g;
                    sum += g;
                }
            }

            // くっきりさ：上下左右との差（ラプラシアン）の分散
            double lapSum = 0, lapSq = 0;
            var count = 0;
            for (var y = 1; y < h - 1; y++)
            {
                for (var x = 1; x < w - 1; x++)
                {
                    var i = y * w + x;
                    double lap = gray[i - 1] + gray[i + 1] + gray[i - w] + gray[i + w] - 4 * gray[i];
                    lapSum += lap;
                    lapSq += lap * lap;
                    count++;
                }
            }
            var mean = count > 0 ? lapSum / count : 0;
            var variance = count > 0 ? lapSq / count - mean * mean : 0;

            return new PhotoQuality
            {
                brightness = (float)(sum / gray.Length),
                sharpness = (float)variance,
            };
        }

        /// <summary>写真を使えるか。使えなければ理由（Dark か FastMotion）を返す。使えれば None。</summary>
        public static ScanReason Judge(PhotoQuality q, float minBrightness, float minSharpness)
        {
            if (q.brightness < minBrightness)
            {
                return ScanReason.Dark;
            }
            if (q.sharpness < minSharpness)
            {
                return ScanReason.FastMotion;
            }
            return ScanReason.None;
        }
    }
}
