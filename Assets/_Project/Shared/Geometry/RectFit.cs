using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Shared
{
    /// <summary>上から見た長方形：中心、幅（長い方）、奥行き（短い方）、回転（度）。</summary>
    public struct RectFitResult
    {
        /// <summary>XZ 平面の中心。x が X、y が Z。</summary>
        public Vector2 center;
        /// <summary>長い方の辺の長さ。</summary>
        public float width;
        /// <summary>短い方の辺の長さ。</summary>
        public float depth;
        /// <summary>
        /// 上から見た回転（度、0 以上 180 未満）。Y 軸まわりの回転で、
        /// Quaternion.Euler(0, yawDegrees, 0) を掛けると、幅の方向（ローカル X）がこの長方形の長い辺に合う。
        /// </summary>
        public float yawDegrees;
    }

    /// <summary>
    /// 点の並びを囲む、いちばん面積の小さい長方形を求める（A の Step 7）。A（机）と B（箱）が使う。
    /// 求め方：凸包を作り、凸包の各辺に沿った長方形のうち、面積が最小のものを選ぶ。
    /// </summary>
    public static class RectFit
    {
        const float Eps = 1e-6f;

        /// <summary>
        /// 点の並びを囲む最小面積の長方形を返す。入力は変えない。
        /// 点が無い・1 つ・全部同じ点のときは、エラーにせず大きさ 0 を返す。
        /// 点が一直線に並ぶときは、その線に沿った幅 &gt; 0、奥行き 0 を返す。
        /// </summary>
        public static RectFitResult Fit(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count == 0)
            {
                return new RectFitResult();
            }

            var hull = ConvexHull(points);
            if (hull.Count == 1)
            {
                return new RectFitResult { center = hull[0] };
            }

            if (hull.Count == 2)
            {
                return FromSegment(hull[0], hull[1]);
            }

            var bestArea = float.MaxValue;
            var best = new RectFitResult();
            for (var i = 0; i < hull.Count; i++)
            {
                var edge = hull[(i + 1) % hull.Count] - hull[i];
                var length = edge.magnitude;
                if (length < Eps)
                {
                    continue;
                }

                var u = edge / length;
                var v = new Vector2(-u.y, u.x);
                float minU = float.MaxValue, maxU = float.MinValue;
                float minV = float.MaxValue, maxV = float.MinValue;
                foreach (var p in hull)
                {
                    var pu = Vector2.Dot(p, u);
                    var pv = Vector2.Dot(p, v);
                    minU = Mathf.Min(minU, pu);
                    maxU = Mathf.Max(maxU, pu);
                    minV = Mathf.Min(minV, pv);
                    maxV = Mathf.Max(maxV, pv);
                }

                var extentU = maxU - minU;
                var extentV = maxV - minV;
                var area = extentU * extentV;
                if (area >= bestArea)
                {
                    continue;
                }

                bestArea = area;
                var center = u * ((minU + maxU) / 2f) + v * ((minV + maxV) / 2f);
                // 長い方の辺を幅にする
                best = extentU >= extentV
                    ? Make(center, extentU, extentV, u)
                    : Make(center, extentV, extentU, v);
            }

            return best;
        }

        static RectFitResult FromSegment(Vector2 a, Vector2 b)
        {
            var d = b - a;
            var length = d.magnitude;
            return Make((a + b) / 2f, length, 0f, length < Eps ? Vector2.right : d / length);
        }

        static RectFitResult Make(Vector2 center, float width, float depth, Vector2 widthAxis)
        {
            // Quaternion.Euler(0, θ, 0) はローカル X を XZ 平面の (cosθ, -sinθ) へ回す
            var yaw = Mathf.Atan2(-widthAxis.y, widthAxis.x) * Mathf.Rad2Deg;
            yaw = ((yaw % 180f) + 180f) % 180f;
            if (yaw >= 180f - Eps)
            {
                yaw = 0f;
            }

            return new RectFitResult { center = center, width = width, depth = depth, yawDegrees = yaw };
        }

        /// <summary>凸包（反時計回り）。点が全部同じなら 1 点、一直線なら両端の 2 点。</summary>
        static List<Vector2> ConvexHull(IReadOnlyList<Vector2> points)
        {
            var sorted = new List<Vector2>(points);
            sorted.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            var unique = new List<Vector2>(sorted.Count);
            foreach (var p in sorted)
            {
                if (unique.Count == 0 || (p - unique[unique.Count - 1]).sqrMagnitude > Eps * Eps)
                {
                    unique.Add(p);
                }
            }

            if (unique.Count <= 2)
            {
                return unique;
            }

            var hull = new List<Vector2>(unique.Count * 2);
            for (var i = 0; i < unique.Count; i++)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], unique[i]) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(unique[i]);
            }

            var lower = hull.Count + 1;
            for (var i = unique.Count - 2; i >= 0; i--)
            {
                while (hull.Count >= lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], unique[i]) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(unique[i]);
            }

            hull.RemoveAt(hull.Count - 1);

            // 一直線に並ぶ点は凸包が 2 点に縮む
            return hull.Count >= 3 ? hull : new List<Vector2> { unique[0], unique[unique.Count - 1] };
        }

        static float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }
    }
}
