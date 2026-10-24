using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>机の長方形と、その上に置く StageRoot の位置と向き。</summary>
    public struct DeskFit
    {
        /// <summary>StageRoot の位置（世界座標）。机の天面の長方形の中心、机の面の高さ。</summary>
        public Vector3 Position;

        /// <summary>StageRoot の Y 軸まわりの回転（度）。ローカル X が長い方の辺に合う。</summary>
        public float YawDegrees;

        /// <summary>長方形の幅（長い方、X）と奥行き（Z）。</summary>
        public Vector2 Size;
    }

    /// <summary>
    /// 机の決定の計算（A_ar.md Step 8）。AR に触れない普通のクラスにして、テストできるようにしてある。
    /// 座標は 00_overview.md 3.1：原点は机の天面の長方形の中心、長い方の辺が X。
    /// </summary>
    public static class DeskFrameMath
    {
        /// <summary>境界の点が、長方形を求めるのに足りる最小の数。</summary>
        public const int MinBoundaryPoints = 3;

        /// <summary>
        /// 机の面の境界（世界座標）から、StageRoot の位置と向き、机の大きさを求める。
        /// 長方形は RectFit（境界の点を囲む最小面積の長方形）で求める。
        /// </summary>
        public static DeskFit FitDesk(IReadOnlyList<Vector3> boundary, float height)
        {
            var points = new List<Vector2>(boundary.Count);
            foreach (var p in boundary)
            {
                points.Add(new Vector2(p.x, p.z));
            }

            var rect = RectFit.Fit(points);
            return new DeskFit
            {
                Position = new Vector3(rect.center.x, height, rect.center.y),
                YawDegrees = rect.yawDegrees,
                Size = new Vector2(rect.width, rect.depth),
            };
        }

        /// <summary>
        /// 机が決まった後の大きさ。原点と向きは変えないので、
        /// 原点を中心に、軸に平行で、境界の点をすべて含む長方形の大きさを返す
        /// （SurfaceStabilizer が「机の長方形」をそう扱う）。
        /// </summary>
        public static Vector2 SymmetricSize(IReadOnlyList<Vector3> boundary, Pose root)
        {
            var inverse = Quaternion.Inverse(root.rotation);
            float maxX = 0f, maxZ = 0f;
            foreach (var p in boundary)
            {
                var local = inverse * (p - root.position);
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxZ = Mathf.Max(maxZ, Mathf.Abs(local.z));
            }

            return new Vector2(maxX * 2f, maxZ * 2f);
        }

        /// <summary>点（XZ）が、境界の点の並びが作る多角形の中にあるか。</summary>
        public static bool ContainsXZ(IReadOnlyList<Vector3> boundary, Vector3 point)
        {
            var inside = false;
            for (int i = 0, j = boundary.Count - 1; i < boundary.Count; j = i++)
            {
                var a = boundary[i];
                var b = boundary[j];
                if ((a.z > point.z) != (b.z > point.z)
                    && point.x < (b.x - a.x) * (point.z - a.z) / (b.z - a.z) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
