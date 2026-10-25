using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>切り抜きの結果。</summary>
    public struct CutoutResult
    {
        public bool Ok;
        public string Reason;
        /// <summary>切り抜いた画像（背景は透明）。Texture2D.SetPixels32 に渡せる並び（添字 0 が左下の行）。</summary>
        public Color32[] Pixels;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// 人形の写真から、人形の部分だけを切り抜く計算（キャラクターを写真で作る）。
    /// 無地の背景（白い紙など）で撮った写真を前提に、写真の縁の色を背景とみなし、縁からつながる、その色に近い部分を透明にする。
    /// 人形の中にある背景と同じ色の部分（縁につながっていない所）は、そのまま残す。
    /// MonoBehaviour から切り離して、EditMode テストで確かめる。
    /// </summary>
    public static class CutoutMath
    {
        // 縁として見る幅（短い辺の割合。最低 2 画素）
        const float BorderRatio = 0.02f;

        // 背景とみなす色の近さ（RGB の距離）。縁の色のばらつきに合わせて、この範囲に収める
        const float MinTolerance = 30f;
        const float MaxTolerance = 90f;
        const float NoiseGain = 2.5f;

        // 背景が見つかったとみなす最小の割合。これより少ないと、背景が無地ではないとみなす
        const float MinBackgroundFraction = 0.05f;

        // 人形とみなす最小の大きさ（写真全体に対する割合）。これより小さい塊は、ごみとして消す
        const float MinForegroundFraction = 0.005f;
        const float MinBlobFraction = 0.002f;

        // 切り抜いた画像の周りの余白（画素）
        const int Margin = 2;

        public static CutoutResult Cut(Color32[] src, int width, int height)
        {
            if (src == null || width < 8 || height < 8 || src.Length < width * height)
            {
                return Fail("写真が小さすぎます");
            }

            var background = BackgroundColor(src, width, height, out var noise);
            var tolerance = Mathf.Clamp(MinTolerance + NoiseGain * noise, MinTolerance, MaxTolerance);
            var tolSq = tolerance * tolerance;

            // 縁から、背景に近い色でつながる部分を塗る
            var isBackground = new bool[width * height];
            var queue = new int[width * height];
            var head = 0;
            var tail = 0;

            void Seed(int x, int y)
            {
                var i = y * width + x;
                if (!isBackground[i] && DistSq(src[i], background) <= tolSq)
                {
                    isBackground[i] = true;
                    queue[tail++] = i;
                }
            }

            for (var x = 0; x < width; x++)
            {
                Seed(x, 0);
                Seed(x, height - 1);
            }
            for (var y = 0; y < height; y++)
            {
                Seed(0, y);
                Seed(width - 1, y);
            }
            while (head < tail)
            {
                var i = queue[head++];
                var x = i % width;
                var y = i / width;
                if (x > 0) Seed(x - 1, y);
                if (x < width - 1) Seed(x + 1, y);
                if (y > 0) Seed(x, y - 1);
                if (y < height - 1) Seed(x, y + 1);
            }

            var total = width * height;
            var backgroundCount = tail;
            if (backgroundCount < total * MinBackgroundFraction)
            {
                return Fail("背景が、一色につながっていません。無地の紙の上で撮ってね");
            }

            // 小さな塊（ごみ）を消す
            RemoveSmallBlobs(isBackground, width, height, Mathf.Max(20, Mathf.RoundToInt(total * MinBlobFraction)));

            // 人形の範囲
            int minX = width, maxX = -1, minY = height, maxY = -1, foreground = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (isBackground[y * width + x])
                    {
                        continue;
                    }
                    foreground++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (foreground < total * MinForegroundFraction)
            {
                return Fail("人形が見つかりません。人形が背景と同じ色かもしれません");
            }

            minX = Mathf.Max(0, minX - Margin);
            minY = Mathf.Max(0, minY - Margin);
            maxX = Mathf.Min(width - 1, maxX + Margin);
            maxY = Mathf.Min(height - 1, maxY + Margin);
            var outWidth = maxX - minX + 1;
            var outHeight = maxY - minY + 1;
            var pixels = new Color32[outWidth * outHeight];
            for (var y = 0; y < outHeight; y++)
            {
                for (var x = 0; x < outWidth; x++)
                {
                    var i = (y + minY) * width + (x + minX);
                    var c = src[i];
                    pixels[y * outWidth + x] = isBackground[i] ? new Color32(c.r, c.g, c.b, 0) : new Color32(c.r, c.g, c.b, 255);
                }
            }

            return new CutoutResult { Ok = true, Reason = "", Pixels = pixels, Width = outWidth, Height = outHeight };
        }

        /// <summary>縁の色の、各色の中央値を背景の色とする。noise は、縁の色の、背景の色からの平均の距離。</summary>
        static Color32 BackgroundColor(Color32[] src, int width, int height, out float noise)
        {
            var ring = Mathf.Max(2, Mathf.RoundToInt(Mathf.Min(width, height) * BorderRatio));
            var rs = new List<byte>();
            var gs = new List<byte>();
            var bs = new List<byte>();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (x >= ring && x < width - ring && y >= ring && y < height - ring)
                    {
                        // 内側は飛ばす（行の途中から縁の右側へ）
                        x = width - ring - 1;
                        continue;
                    }
                    var c = src[y * width + x];
                    rs.Add(c.r);
                    gs.Add(c.g);
                    bs.Add(c.b);
                }
            }
            rs.Sort();
            gs.Sort();
            bs.Sort();
            var mid = rs.Count / 2;
            var background = new Color32(rs[mid], gs[mid], bs[mid], 255);

            double sum = 0;
            for (var i = 0; i < rs.Count; i++)
            {
                sum += Mathf.Sqrt(DistSq(new Color32(rs[i], gs[i], bs[i], 255), background));
            }
            noise = rs.Count > 0 ? (float)(sum / rs.Count) : 0f;
            return background;
        }

        static float DistSq(Color32 a, Color32 b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        /// <summary>背景ではない画素のつながり（上下左右）のうち、minSize より小さい物を背景にする。</summary>
        static void RemoveSmallBlobs(bool[] isBackground, int width, int height, int minSize)
        {
            var seen = new bool[isBackground.Length];
            var stack = new int[isBackground.Length];
            var blob = new List<int>();
            for (var start = 0; start < isBackground.Length; start++)
            {
                if (isBackground[start] || seen[start])
                {
                    continue;
                }
                blob.Clear();
                var top = 0;
                stack[top++] = start;
                seen[start] = true;
                while (top > 0)
                {
                    var i = stack[--top];
                    blob.Add(i);
                    var x = i % width;
                    var y = i / width;
                    TryPush(i - 1, x > 0);
                    TryPush(i + 1, x < width - 1);
                    TryPush(i - width, y > 0);
                    TryPush(i + width, y < height - 1);
                }
                if (blob.Count < minSize)
                {
                    foreach (var i in blob)
                    {
                        isBackground[i] = true;
                    }
                }

                void TryPush(int j, bool inside)
                {
                    if (inside && !isBackground[j] && !seen[j])
                    {
                        seen[j] = true;
                        stack[top++] = j;
                    }
                }
            }
        }

        static CutoutResult Fail(string reason) => new CutoutResult { Ok = false, Reason = reason };
    }
}
