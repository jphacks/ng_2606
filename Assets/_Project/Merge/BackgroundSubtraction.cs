using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 背景だけの写真と、人形を置いた写真の差から、人形を取り出す（キャラクターを写真で作る）。
    /// 背景が汚い・模様があっても、同じ場所で、背景だけを先に撮れば、人形だけが残る。
    /// 次のずれに、ある程度、耐える：
    ///  ・手のぶれ（全体のずれ）：写真の縁（人形がいない所）で、いちばん合うずれを探す
    ///  ・明るさの変化（カメラの自動の明るさ）：縁の色の差の中央値を、背景に足す
    ///  ・細かいずれ：1〜2 画素の近くで、いちばん近い色との差を使う
    /// 結果は、少し広げた人型の枠（HumanMask）の内側に限る。人形の周りの余白は切り落とす。
    /// 写真は、2 枚とも同じ大きさ。添字 0 が左下の行。
    /// MonoBehaviour から切り離して、EditMode テストで確かめる。
    /// </summary>
    public static class BackgroundSubtraction
    {
        // 手のぶれを探す範囲（画素）
        const int MaxShift = 6;

        // 細かいずれに耐える近さ（画素）
        const int Near = 2;

        // 縁として見る幅（短い辺の割合）。人形は、縁にはかからない前提
        const float BorderRatio = 0.06f;

        // 「違う」とみなす色の差（RGB の距離）。縁の差のばらつきに合わせて、この範囲に収める
        const float MinTolerance = 32f;
        const float MaxTolerance = 80f;

        // 枠を広げる割合（人形が枠のふちで欠けないように）
        const float FrameGrow = 0.10f;

        // 小さな塊（ごみ）と穴の大きさ（枠の面積に対する割合）
        const float MinBlobFraction = 0.01f;
        const float MaxHoleFraction = 0.02f;

        // 人形とみなす最小と最大の大きさ（枠の面積に対する割合）
        const float MinFound = 0.04f;
        const float MaxFound = 0.92f;

        // 切り抜きの周りの余白（画素）
        const int Margin = 2;

        public static CutoutResult Cut(Color32[] background, Color32[] photo, int width, int height)
        {
            if (background == null || photo == null || width < 16 || height < 16
                || background.Length < width * height || photo.Length < width * height)
            {
                return Fail("写真が ただしく ありません");
            }

            // 1. 縁で、全体の明るさの変化と、手のぶれを探す
            var ring = Mathf.Max(3, Mathf.RoundToInt(Mathf.Min(width, height) * BorderRatio));
            var borderPixels = BorderIndices(width, height, ring, 3);
            var offset = BrightnessOffset(background, photo, borderPixels);
            FindShift(background, photo, width, height, borderPixels, offset, out var shiftX, out var shiftY);

            // 2. 画素ごとの、背景との色の差
            var distance = new float[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    distance[y * width + x] = NearestDistance(background, photo, width, height, x, y, shiftX, shiftY, offset);
                }
            }

            // 3. 縁の差のばらつきから、「違う」とみなす差を決める
            var borderDistances = new List<float>(borderPixels.Count);
            foreach (var i in borderPixels) borderDistances.Add(distance[i]);
            borderDistances.Sort();
            var noise = borderDistances.Count > 0 ? borderDistances[borderDistances.Count / 2] : 0f;
            var tolerance = Mathf.Clamp(MinTolerance + 3f * noise, MinTolerance, MaxTolerance);

            // 4. 違う所のうち、広げた人型の枠の中だけを残す
            var mask = new bool[width * height];
            var frameArea = 0;
            for (var y = 0; y < height; y++)
            {
                var v = (y + 0.5f) / height;
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / width;
                    if (HumanMask.Coverage(u, v, FrameGrow) < 0.5f)
                    {
                        continue;
                    }
                    frameArea++;
                    if (distance[y * width + x] > tolerance)
                    {
                        mask[y * width + x] = true;
                    }
                }
            }

            // 5. 整える：小さな穴を埋め、小さな塊を消す
            FillSmallHoles(mask, width, height, Mathf.Max(8, Mathf.RoundToInt(frameArea * MaxHoleFraction)));
            RemoveSmallBlobs(mask, width, height, Mathf.Max(8, Mathf.RoundToInt(frameArea * MinBlobFraction)));

            // 6. 人形の範囲
            int minX = width, maxX = -1, minY = height, maxY = -1, found = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!mask[y * width + x]) continue;
                    found++;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            var fraction = frameArea > 0 ? found / (float)frameArea : 0f;
            if (fraction < MinFound)
            {
                return Fail("にんぎょうが みつかりません。はいけいと にんぎょうの ちがいが ちいさいみたい");
            }
            if (fraction > MaxFound)
            {
                return Fail("はいけいが ちがいます。スマホを うごかさないで とりなおしてね");
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
                    var c = photo[i];
                    pixels[y * outWidth + x] = new Color32(c.r, c.g, c.b, mask[i] ? (byte)255 : (byte)0);
                }
            }
            return new CutoutResult { Ok = true, Reason = "", Pixels = pixels, Width = outWidth, Height = outHeight };
        }

        /// <summary>縁の画素の添字（step 画素おき）。</summary>
        static List<int> BorderIndices(int width, int height, int ring, int step)
        {
            var list = new List<int>();
            for (var y = 0; y < height; y += step)
            {
                for (var x = 0; x < width; x += step)
                {
                    if (x < ring || x >= width - ring || y < ring || y >= height - ring)
                    {
                        list.Add(y * width + x);
                    }
                }
            }
            return list;
        }

        /// <summary>縁での、写真 − 背景 の色の差の中央値（各色）。背景に足すと、全体の明るさの変化を打ち消せる。</summary>
        static Vector3 BrightnessOffset(Color32[] background, Color32[] photo, List<int> border)
        {
            var r = new List<int>(border.Count);
            var g = new List<int>(border.Count);
            var b = new List<int>(border.Count);
            foreach (var i in border)
            {
                r.Add(photo[i].r - background[i].r);
                g.Add(photo[i].g - background[i].g);
                b.Add(photo[i].b - background[i].b);
            }
            r.Sort();
            g.Sort();
            b.Sort();
            var mid = border.Count / 2;
            return border.Count == 0 ? Vector3.zero : new Vector3(r[mid], g[mid], b[mid]);
        }

        /// <summary>縁で、写真と背景がいちばん合う、手のぶれ（写真の (x, y) には、背景の (x + dx, y + dy) が対応）を探す。</summary>
        static void FindShift(Color32[] background, Color32[] photo, int width, int height, List<int> border, Vector3 offset,
            out int shiftX, out int shiftY)
        {
            shiftX = 0;
            shiftY = 0;
            var best = float.MaxValue;
            for (var dy = -MaxShift; dy <= MaxShift; dy++)
            {
                for (var dx = -MaxShift; dx <= MaxShift; dx++)
                {
                    float sum = 0f;
                    foreach (var i in border)
                    {
                        var x = i % width;
                        var y = i / width;
                        sum += Distance(photo[i], background[Clamp(y + dy, height) * width + Clamp(x + dx, width)], offset);
                    }
                    // ずれが同じくらいなら、小さいほうを選ぶ
                    sum += (Mathf.Abs(dx) + Mathf.Abs(dy)) * 0.01f * border.Count;
                    if (sum < best)
                    {
                        best = sum;
                        shiftX = dx;
                        shiftY = dy;
                    }
                }
            }
        }

        static float NearestDistance(Color32[] background, Color32[] photo, int width, int height, int x, int y,
            int shiftX, int shiftY, Vector3 offset)
        {
            var c = photo[y * width + x];
            var best = float.MaxValue;
            for (var oy = -Near; oy <= Near; oy++)
            {
                for (var ox = -Near; ox <= Near; ox++)
                {
                    var bx = Clamp(x + shiftX + ox, width);
                    var by = Clamp(y + shiftY + oy, height);
                    var d = Distance(c, background[by * width + bx], offset);
                    if (d < best) best = d;
                }
            }
            return best;
        }

        static float Distance(Color32 photo, Color32 background, Vector3 offset)
        {
            var dr = photo.r - (background.r + offset.x);
            var dg = photo.g - (background.g + offset.y);
            var db = photo.b - (background.b + offset.z);
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        static int Clamp(int v, int size) => v < 0 ? 0 : (v >= size ? size - 1 : v);

        /// <summary>印のない画素のつながり（上下左右）のうち、縁につながらず、size より小さい物（穴）を埋める。</summary>
        static void FillSmallHoles(bool[] mask, int width, int height, int maxSize)
        {
            var seen = new bool[mask.Length];
            var stack = new int[mask.Length];
            var blob = new List<int>();
            for (var start = 0; start < mask.Length; start++)
            {
                if (mask[start] || seen[start])
                {
                    continue;
                }
                blob.Clear();
                var touchesBorder = false;
                var top = 0;
                stack[top++] = start;
                seen[start] = true;
                while (top > 0)
                {
                    var i = stack[--top];
                    blob.Add(i);
                    var x = i % width;
                    var y = i / width;
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1) touchesBorder = true;
                    Push(i - 1, x > 0);
                    Push(i + 1, x < width - 1);
                    Push(i - width, y > 0);
                    Push(i + width, y < height - 1);
                }
                if (!touchesBorder && blob.Count <= maxSize)
                {
                    foreach (var i in blob) mask[i] = true;
                }

                void Push(int j, bool inside)
                {
                    if (inside && !mask[j] && !seen[j])
                    {
                        seen[j] = true;
                        stack[top++] = j;
                    }
                }
            }
        }

        /// <summary>印のある画素のつながり（上下左右）のうち、size より小さい物を消す。</summary>
        static void RemoveSmallBlobs(bool[] mask, int width, int height, int minSize)
        {
            var seen = new bool[mask.Length];
            var stack = new int[mask.Length];
            var blob = new List<int>();
            for (var start = 0; start < mask.Length; start++)
            {
                if (!mask[start] || seen[start])
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
                    Push(i - 1, x > 0);
                    Push(i + 1, x < width - 1);
                    Push(i - width, y > 0);
                    Push(i + width, y < height - 1);
                }
                if (blob.Count < minSize)
                {
                    foreach (var i in blob) mask[i] = false;
                }

                void Push(int j, bool inside)
                {
                    if (inside && mask[j] && !seen[j])
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
