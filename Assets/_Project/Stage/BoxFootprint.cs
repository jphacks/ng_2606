using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>上から見た箱の形：中心、幅（X）、奥行き（Z）、回転（度）。</summary>
    public struct Footprint
    {
        public Vector2 center;
        public float width;
        public float depth;
        public float yawDegrees;
    }

    /// <summary>
    /// 上面の境界の点の並びから、上から見た箱の形を求める（B の Step 6）。
    /// 【仮】今は回転を考えず、XZ の軸にそろえた長方形で囲むだけ。
    /// A の RectFit（Shared/Geometry、A の Step 7）ができたら、Fit の中身をそれに差し替える。
    /// 差し替えるのはこの1か所だけにする。
    /// </summary>
    public static class BoxFootprint
    {
        public static Footprint Fit(Vector2[] outline)
        {
            if (outline == null || outline.Length == 0)
            {
                return new Footprint();
            }

            var min = outline[0];
            var max = outline[0];
            foreach (var p in outline)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return new Footprint
            {
                center = (min + max) / 2f,
                width = max.x - min.x,
                depth = max.y - min.y,
                yawDegrees = 0f,
            };
        }
    }
}
