using System;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 写真から、箱の上面や机の絵を切り出す計算（合流のつなぎで 3D のステージに貼る）。
    /// MonoBehaviour から切り離して、EditMode テストで確かめる。
    /// 写真の座標は、左上が (0, 0)、右下が (1, 1) の割合（x は右へ、y は下へ）。
    /// 箱や机の「面」の座標は、面の真上から見て、右が +X、奥が +Z。
    /// </summary>
    public static class PhotoTextureMath
    {
        /// <summary>面に当てる四隅の並び（面の右が +X、奥が +Z として、左奥、右奥、右手前、左手前）。</summary>
        public const int FarLeft = 0, FarRight = 1, NearRight = 2, NearLeft = 3;

        /// <summary>
        /// 答えの四隅を、使える形にする。8つとも 0（分からない）、0〜1 の外、数字でない、面積がほぼ 0 のときは null。
        /// </summary>
        public static Vector2[] ValidQuad(LlmQuad quad)
        {
            if (quad == null)
            {
                return null;
            }
            var points = quad.ToPoints();
            var allZero = true;
            foreach (var p in points)
            {
                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y))
                {
                    return null;
                }
                if (p.x < -0.001f || p.x > 1.001f || p.y < -0.001f || p.y > 1.001f)
                {
                    return null;
                }
                if (p.x != 0f || p.y != 0f)
                {
                    allZero = false;
                }
            }
            if (allZero || Mathf.Abs(PolygonArea(points)) < 0.0005f)
            {
                return null;
            }
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(Mathf.Clamp01(points[i].x), Mathf.Clamp01(points[i].y));
            }
            return points;
        }

        static float PolygonArea(Vector2[] p)
        {
            var sum = 0f;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                sum += p[j].x * p[i].y - p[i].x * p[j].y;
            }
            return sum / 2f;
        }

        /// <summary>
        /// 写真の四隅（どの角から、どちら回りでもよい）を、面の左奥、右奥、右手前、左手前の順に並べ直す。
        /// 写真は真上から撮った物として、面の +X を写真の右、面の +Z を写真の上とみなす。
        /// 面は、真上から見て yawUnityDegrees（Unity の Y 軸まわり。上から見て時計回り）だけ回っていて、幅 width（X）、奥行き depth（Z）。
        /// 角の向きを比べて、いちばん合う組み合わせを選ぶ。
        /// </summary>
        public static Vector2[] OrderForFace(Vector2[] quad, float width, float depth, float yawUnityDegrees)
        {
            if (quad == null || quad.Length != 4)
            {
                throw new ArgumentException("四隅は 4 つ必要です", nameof(quad));
            }

            // 面の四隅の向き（ステージの XZ → 写真の向き：x は同じ、y は Z の逆）
            var rad = yawUnityDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            var hx = Mathf.Max(width, 1e-4f) / 2f;
            var hz = Mathf.Max(depth, 1e-4f) / 2f;
            var local = new[]
            {
                new Vector2(-hx, hz), new Vector2(hx, hz), new Vector2(hx, -hz), new Vector2(-hx, -hz),
            };
            var cornerAngles = new float[4];
            for (var k = 0; k < 4; k++)
            {
                // Unity の Euler(0, θ, 0)：ローカル (lx, lz) → ステージ (lx cosθ + lz sinθ, −lx sinθ + lz cosθ)
                var sx = local[k].x * cos + local[k].y * sin;
                var sz = -local[k].x * sin + local[k].y * cos;
                cornerAngles[k] = Mathf.Atan2(-sz, sx);
            }

            var centroid = Vector2.zero;
            foreach (var p in quad) centroid += p;
            centroid /= 4f;
            var quadAngles = new float[4];
            for (var j = 0; j < 4; j++)
            {
                quadAngles[j] = Mathf.Atan2(quad[j].y - centroid.y, quad[j].x - centroid.x);
            }

            // 四隅を角度の順に並べる（昇順。写真の座標は y が下向きなので、画面では時計回り）
            var quadOrder = new[] { 0, 1, 2, 3 };
            Array.Sort(quadOrder, (a, b) => quadAngles[a].CompareTo(quadAngles[b]));
            // 面の角を、同じ向きに並べる（local の添字）
            var cornerOrder = new[] { 0, 1, 2, 3 };
            Array.Sort(cornerOrder, (a, b) => cornerAngles[a].CompareTo(cornerAngles[b]));

            // 巡回のずらし方のうち、角度の差の合計がいちばん小さいものを選ぶ
            var bestShift = 0;
            var bestCost = float.MaxValue;
            for (var shift = 0; shift < 4; shift++)
            {
                var cost = 0f;
                for (var i = 0; i < 4; i++)
                {
                    var diff = quadAngles[quadOrder[(i + shift) % 4]] - cornerAngles[cornerOrder[i]];
                    cost += Mathf.Abs(Mathf.DeltaAngle(diff * Mathf.Rad2Deg, 0f));
                }
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestShift = shift;
                }
            }

            var result = new Vector2[4];
            for (var i = 0; i < 4; i++)
            {
                result[cornerOrder[i]] = quad[quadOrder[(i + bestShift) % 4]];
            }
            return result;
        }

        /// <summary>
        /// 写真から、四隅（左奥、右奥、右手前、左手前の順）で囲まれた部分を、outWidth x outHeight に切り出す。
        /// 結果は Texture2D.SetPixels32 にそのまま渡せる並び（添字 0 が左下の行）。上が奥、右が +X。
        /// src も、Texture2D.GetPixels32 の並び（添字 0 が左下の行）。
        /// </summary>
        public static Color32[] Crop(Color32[] src, int srcWidth, int srcHeight, Vector2[] corners, int outWidth, int outHeight)
        {
            if (corners == null || corners.Length != 4)
            {
                throw new ArgumentException("四隅は 4 つ必要です", nameof(corners));
            }
            var result = new Color32[outWidth * outHeight];
            for (var iy = 0; iy < outHeight; iy++)
            {
                var v = (iy + 0.5f) / outHeight; // 0 が手前、1 が奥
                for (var ix = 0; ix < outWidth; ix++)
                {
                    var u = (ix + 0.5f) / outWidth;
                    var near = Vector2.Lerp(corners[NearLeft], corners[NearRight], u);
                    var far = Vector2.Lerp(corners[FarLeft], corners[FarRight], u);
                    var p = Vector2.Lerp(near, far, v);
                    result[iy * outWidth + ix] = Sample(src, srcWidth, srcHeight, p);
                }
            }
            return result;
        }

        /// <summary>写真の (x, y)（左上が 0、下向きが +y の割合）を、双一次補間で読む。</summary>
        static Color32 Sample(Color32[] src, int width, int height, Vector2 p)
        {
            var fx = Mathf.Clamp(p.x * width - 0.5f, 0f, width - 1f);
            var fyTop = Mathf.Clamp(p.y * height - 0.5f, 0f, height - 1f);
            var x0 = Mathf.FloorToInt(fx);
            var y0 = Mathf.FloorToInt(fyTop);
            var x1 = Mathf.Min(x0 + 1, width - 1);
            var y1 = Mathf.Min(y0 + 1, height - 1);
            var tx = fx - x0;
            var ty = fyTop - y0;

            // 配列は下の行が先。写真の上から y 行目は、配列の (height − 1 − y) 行目
            Color32 At(int x, int yTop) => src[(height - 1 - yTop) * width + x];
            var c00 = At(x0, y0);
            var c10 = At(x1, y0);
            var c01 = At(x0, y1);
            var c11 = At(x1, y1);
            byte Mix(byte a, byte b, byte c, byte d)
            {
                var top = a + (b - a) * tx;
                var bottom = c + (d - c) * tx;
                return (byte)Mathf.RoundToInt(top + (bottom - top) * ty);
            }
            return new Color32(Mix(c00.r, c10.r, c01.r, c11.r), Mix(c00.g, c10.g, c01.g, c11.g), Mix(c00.b, c10.b, c01.b, c11.b), 255);
        }

        /// <summary>画素の平均の色（箱の側面の色に使う）。</summary>
        public static Color32 Average(Color32[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return new Color32(180, 180, 180, 255);
            }
            long r = 0, g = 0, b = 0;
            foreach (var c in pixels)
            {
                r += c.r;
                g += c.g;
                b += c.b;
            }
            var n = pixels.Length;
            return new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }
    }
}
