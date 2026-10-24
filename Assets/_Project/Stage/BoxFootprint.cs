using DeskStage.Shared;
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
    /// A の RectFit（Shared/Geometry）で、点を囲むいちばん面積の小さい長方形にする。
    /// 長い方の辺が幅（ローカル X）になり、回転は 0 以上 180 未満。
    /// </summary>
    public static class BoxFootprint
    {
        public static Footprint Fit(Vector2[] outline)
        {
            var r = RectFit.Fit(outline);
            return new Footprint
            {
                center = r.center,
                width = r.width,
                depth = r.depth,
                yawDegrees = r.yawDegrees,
            };
        }
    }
}
