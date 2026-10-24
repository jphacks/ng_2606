using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// 面の安定化（SurfaceStabilizerCore）で使う、上から見た多角形の計算。
    /// 点は XZ 平面（x が X、y が Z）。
    /// </summary>
    public static class SurfaceGeometry
    {
        /// <summary>符号つき面積。反時計回りなら正。</summary>
        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            var sum = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                sum += polygon[j].x * polygon[i].y - polygon[i].x * polygon[j].y;
            }

            return sum / 2f;
        }

        public static float Area(IReadOnlyList<Vector2> polygon) => Mathf.Abs(SignedArea(polygon));

        /// <summary>重心。面積が 0 に近いときは点の平均。</summary>
        public static Vector2 Centroid(IReadOnlyList<Vector2> polygon)
        {
            var signed = SignedArea(polygon);
            if (Mathf.Abs(signed) < 1e-9f)
            {
                var mean = Vector2.zero;
                foreach (var p in polygon) mean += p;
                return polygon.Count > 0 ? mean / polygon.Count : Vector2.zero;
            }

            float cx = 0f, cy = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var cross = polygon[j].x * polygon[i].y - polygon[i].x * polygon[j].y;
                cx += (polygon[j].x + polygon[i].x) * cross;
                cy += (polygon[j].y + polygon[i].y) * cross;
            }

            return new Vector2(cx, cy) / (6f * signed);
        }

        public static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>2 つの多角形のすき間（m）。重なっている、または一方がもう一方の中にあれば 0。</summary>
        public static float Distance(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
        {
            if (a.Count == 0 || b.Count == 0)
            {
                return float.MaxValue;
            }

            foreach (var p in a)
            {
                if (Contains(b, p)) return 0f;
            }

            foreach (var p in b)
            {
                if (Contains(a, p)) return 0f;
            }

            var min = float.MaxValue;
            for (int i = 0, j = a.Count - 1; i < a.Count; j = i++)
            {
                for (int k = 0, l = b.Count - 1; k < b.Count; l = k++)
                {
                    min = Mathf.Min(min, SegmentDistance(a[j], a[i], b[l], b[k]));
                    if (min <= 0f) return 0f;
                }
            }

            return min;
        }

        static float SegmentDistance(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            if (SegmentsIntersect(a1, a2, b1, b2))
            {
                return 0f;
            }

            return Mathf.Min(
                Mathf.Min(PointSegmentDistance(a1, b1, b2), PointSegmentDistance(a2, b1, b2)),
                Mathf.Min(PointSegmentDistance(b1, a1, a2), PointSegmentDistance(b2, a1, a2)));
        }

        static float PointSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            var t = lengthSq < 1e-12f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            return (p - (a + ab * t)).magnitude;
        }

        static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            var d1 = Cross(b1, b2, a1);
            var d2 = Cross(b1, b2, a2);
            var d3 = Cross(a1, a2, b1);
            var d4 = Cross(a1, a2, b2);
            return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f))
                && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
        }

        static float Cross(Vector2 o, Vector2 a, Vector2 b)
            => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        /// <summary>凸包（反時計回り）。点が 3 つ未満なら、重複を除いた点をそのまま返す。</summary>
        public static Vector2[] ConvexHull(IReadOnlyList<Vector2> points)
        {
            var sorted = new List<Vector2>(points);
            sorted.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
            var unique = new List<Vector2>(sorted.Count);
            foreach (var p in sorted)
            {
                if (unique.Count == 0 || (p - unique[unique.Count - 1]).sqrMagnitude > 1e-12f)
                {
                    unique.Add(p);
                }
            }

            if (unique.Count < 3)
            {
                return unique.ToArray();
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
            return hull.ToArray();
        }

        /// <summary>
        /// 各辺を外へ distance だけ広げた多角形を返す（反時計回りで返す）。
        /// 角は辺の外向きの法線の和で動かす（とがった角で飛び出しすぎないよう、上限をかける）。
        /// </summary>
        public static Vector2[] Expand(IReadOnlyList<Vector2> polygon, float distance)
        {
            var points = new List<Vector2>(polygon);
            if (SignedArea(points) < 0f)
            {
                points.Reverse();
            }

            var n = points.Count;
            if (n < 3 || distance == 0f)
            {
                return points.ToArray();
            }

            var result = new Vector2[n];
            for (var i = 0; i < n; i++)
            {
                var prev = points[(i + n - 1) % n];
                var cur = points[i];
                var next = points[(i + 1) % n];
                var n1 = OutwardNormal(prev, cur);
                var n2 = OutwardNormal(cur, next);
                var denominator = Mathf.Max(1f + Vector2.Dot(n1, n2), 0.2f);
                result[i] = cur + (n1 + n2) / denominator * distance;
            }

            return result;
        }

        static Vector2 OutwardNormal(Vector2 from, Vector2 to)
        {
            // 反時計回りの多角形では、辺の向きの右手側が外
            var d = to - from;
            var length = d.magnitude;
            return length < 1e-9f ? Vector2.zero : new Vector2(d.y, -d.x) / length;
        }
    }
}
