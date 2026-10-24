using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// スタートからゴールへ行けるかの簡単な調べ方（C_gameplay.md の Step 12 の 7）。
    /// 机の天面と箱の上面を「場所」とし、通れる道だけをたどってゴールの場所に着けるかを見る。
    /// - 箱はすべて机の上に立っているので、机の天面とどの箱もつながっているとみなす
    /// - 箱どうしは、くっついているか、すき間が AdjacentGap 以下ならつながっている
    /// - 上りは StepRules で Wall でなければ通れる（歩く・ジャンプ）。下りはいくら高くても通れる
    /// すき間をジャンプで飛び越える動きと、まわりを全部ほかの箱に囲まれた箱は考えない（割り切り）。
    /// </summary>
    public static class CourseReachability
    {
        /// <summary>このすき間以下なら、隣の箱とつながっているとみなす（m）</summary>
        public const float AdjacentGap = 0.02f;

        /// <summary>点がその面の上にあるとみなす高さの誤差（m）</summary>
        public const float OnFaceTolerance = 0.005f;

        struct Face
        {
            public float top;
            // 上から見た四隅（XZ）。机は null で、机の長方形を使う
            public Vector2[] corners;
        }

        /// <summary>
        /// スタートの点からゴールの点へ行けるか。どちらかの点がどの面の上にもなければ、判定せずに true を返す。
        /// </summary>
        public static bool CanReach(StageData stage, StepRules rules, Vector3 start, Vector3 goal)
        {
            var faces = BuildFaces(stage);
            int from = FaceAt(faces, start);
            int to = FaceAt(faces, goal);
            if (from < 0 || to < 0)
            {
                return true;
            }

            // 通れる道だけをたどる（幅優先）
            var visited = new bool[faces.Count];
            var queue = new Queue<int>();
            visited[from] = true;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int a = queue.Dequeue();
                if (a == to)
                {
                    return true;
                }
                for (int b = 0; b < faces.Count; b++)
                {
                    if (visited[b] || b == a || !Connected(faces, a, b))
                    {
                        continue;
                    }
                    float rise = faces[b].top - faces[a].top;
                    if (rise > 0f && rules.Classify(rise) == StepKind.Wall)
                    {
                        continue;
                    }
                    visited[b] = true;
                    queue.Enqueue(b);
                }
            }
            return false;
        }

        // 0 番は机の天面、1 番からは箱の上面
        static List<Face> BuildFaces(StageData stage)
        {
            float hw = stage.desk.width / 2f, hd = stage.desk.depth / 2f;
            var faces = new List<Face>
            {
                new Face
                {
                    top = 0f,
                    corners = new[] { new Vector2(-hw, -hd), new Vector2(hw, -hd), new Vector2(hw, hd), new Vector2(-hw, hd) },
                },
            };
            foreach (var box in stage.boxes)
            {
                Vector3 c = Vec3.To(box.center);
                Vector3 s = Vec3.To(box.size);
                var rot = Quaternion.Euler(0f, box.yawDegrees, 0f);
                var corners = new Vector2[4];
                var signs = new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
                for (int i = 0; i < 4; i++)
                {
                    var p = rot * new Vector3(signs[i].x * s.x / 2f, 0f, signs[i].y * s.z / 2f);
                    corners[i] = new Vector2(c.x + p.x, c.z + p.z);
                }
                faces.Add(new Face { top = s.y, corners = corners });
            }
            return faces;
        }

        // 点がある面（上から見て中にあり、高さが合うもの）。いくつもあれば一番高い面
        static int FaceAt(List<Face> faces, Vector3 point)
        {
            var xz = new Vector2(point.x, point.z);
            int best = -1;
            for (int i = 0; i < faces.Count; i++)
            {
                if (Mathf.Abs(point.y - faces[i].top) <= OnFaceTolerance && Inside(faces[i].corners, xz)
                    && (best < 0 || faces[i].top > faces[best].top))
                {
                    best = i;
                }
            }
            return best;
        }

        static bool Connected(List<Face> faces, int a, int b)
        {
            // 机の天面は、どの箱ともつながっている
            if (a == 0 || b == 0)
            {
                return true;
            }
            return Distance(faces[a].corners, faces[b].corners) <= AdjacentGap;
        }

        // 2つの四角形（上から見た形）の間の距離。重なっていれば 0
        public static float Distance(Vector2[] p, Vector2[] q)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Inside(q, p[i]) || Inside(p, q[i]))
                {
                    return 0f;
                }
                for (int j = 0; j < 4; j++)
                {
                    if (SegmentsCross(p[i], p[(i + 1) % 4], q[j], q[(j + 1) % 4]))
                    {
                        return 0f;
                    }
                }
            }
            float min = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    min = Mathf.Min(min, PointToSegment(p[i], q[j], q[(j + 1) % 4]));
                    min = Mathf.Min(min, PointToSegment(q[i], p[j], p[(j + 1) % 4]));
                }
            }
            return min;
        }

        // 凸な四角形の中（ふちを含む）にあるか
        static bool Inside(Vector2[] poly, Vector2 pt)
        {
            bool? sign = null;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                float cross = (b.x - a.x) * (pt.y - a.y) - (b.y - a.y) * (pt.x - a.x);
                if (Mathf.Abs(cross) < 1e-9f)
                {
                    continue;
                }
                bool s = cross > 0f;
                if (sign == null) sign = s;
                else if (sign != s) return false;
            }
            return true;
        }

        static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float d1 = Cross(c, d, a), d2 = Cross(c, d, b), d3 = Cross(a, b, c), d4 = Cross(a, b, d);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        static float PointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
