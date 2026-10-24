using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// スキャン画面の演出で使う計算。計算だけの普通のクラス（Step 8）。
    /// </summary>
    public static class ScanGeometry
    {
        /// <summary>
        /// 上から見た形（XZ の点の並び）を、指定した高さの平らな板の三角形にする。
        /// 中心から扇形に分けるので、凸な形（A の上面）向け。点が3つ未満なら空。
        /// </summary>
        public static void FanMesh(IReadOnlyList<Vector2> outline, float height, List<Vector3> vertices, List<int> triangles)
        {
            vertices.Clear();
            triangles.Clear();
            if (outline == null || outline.Count < 3)
            {
                return;
            }
            var center = Vector2.zero;
            foreach (var p in outline)
            {
                center += p;
            }
            center /= outline.Count;
            vertices.Add(new Vector3(center.x, height, center.y));
            foreach (var p in outline)
            {
                vertices.Add(new Vector3(p.x, height, p.y));
            }
            // 点の並びが時計回りか反時計回りかで、上から見える向きにそろえる
            var clockwise = SignedArea(outline) < 0f;
            for (int i = 0; i < outline.Count; i++)
            {
                var a = 1 + i;
                var b = 1 + (i + 1) % outline.Count;
                if (clockwise)
                {
                    triangles.Add(0);
                    triangles.Add(a);
                    triangles.Add(b);
                }
                else
                {
                    triangles.Add(0);
                    triangles.Add(b);
                    triangles.Add(a);
                }
            }
        }

        /// <summary>多角形の符号付き面積（XZ を x・y とみなす。反時計回りで正）。</summary>
        public static float SignedArea(IReadOnlyList<Vector2> outline)
        {
            var sum = 0f;
            for (int i = 0; i < outline.Count; i++)
            {
                var p = outline[i];
                var q = outline[(i + 1) % outline.Count];
                sum += p.x * q.y - q.x * p.y;
            }
            return sum * 0.5f;
        }

        /// <summary>
        /// 机の長方形（中心が原点、幅 size.x・奥行き size.y）のふちを、t（0〜1）の割合までなぞった点の並び。
        /// 手前の左の角から時計回り（上から見て）になぞる。
        /// </summary>
        public static List<Vector3> TraceRectangle(Vector2 size, float t, float height)
        {
            var hx = size.x * 0.5f;
            var hz = size.y * 0.5f;
            var corners = new[]
            {
                new Vector3(-hx, height, -hz),
                new Vector3(-hx, height, hz),
                new Vector3(hx, height, hz),
                new Vector3(hx, height, -hz),
                new Vector3(-hx, height, -hz),
            };
            var result = new List<Vector3> { corners[0] };
            var total = 2f * (size.x + size.y) * Mathf.Clamp01(t);
            for (int i = 0; i < 4 && total > 0f; i++)
            {
                var length = Vector3.Distance(corners[i], corners[i + 1]);
                if (total >= length)
                {
                    result.Add(corners[i + 1]);
                    total -= length;
                }
                else
                {
                    result.Add(Vector3.Lerp(corners[i], corners[i + 1], length > 0f ? total / length : 1f));
                    total = 0f;
                }
            }
            return result;
        }

        /// <summary>「ぽん」と伸びる動き。0→1 で、少し行き過ぎてから戻る（終わりはちょうど1）。</summary>
        public static float PopCurve(float t)
        {
            t = Mathf.Clamp01(t);
            const float s = 1.7f;
            var u = t - 1f;
            return 1f + (s + 1f) * u * u * u + s * u * u;
        }
    }
}
