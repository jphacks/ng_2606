using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// たしかめ画面の段の印を決める。計算だけの普通のクラス（Step 10）。
    /// 箱ごとに、まわりの面（机の板＝高さ0、隣の箱の上面）との高さの差のうち、いちばん小さいもので判定する。
    /// 値は Docs/StepRules.md（C の StepRules）と同じ。C のクラスは直接呼べないため、ここに同じ決まりを置く。
    /// </summary>
    public static class StepMarkLogic
    {
        public enum Mark
        {
            /// <summary>歩いて登れる（印は出さない）</summary>
            Walk,
            /// <summary>ジャンプで登れる（緑の印）</summary>
            Jump,
            /// <summary>登れない壁（赤の印）</summary>
            Wall,
        }

        /// <summary>歩ける段の上限（m）。0.8cm</summary>
        public const float WalkMax = 0.008f;
        /// <summary>跳べる段の上限（m）。4.5cm。境目ちょうどは Jump</summary>
        public const float JumpMax = 0.045f;
        /// <summary>隣とみなす距離（m）。B の箱と箱のすき間は 0 か 1.6cm 以上なので、それより狭くする</summary>
        public const float NeighborMargin = 0.01f;

        public struct Box
        {
            /// <summary>上から見た範囲（ステージ座標の XZ）</summary>
            public Rect Footprint;
            /// <summary>上面の高さ（m）</summary>
            public float Height;
        }

        public static Mark Classify(float difference)
        {
            var d = Mathf.Abs(difference);
            if (d < WalkMax)
            {
                return Mark.Walk;
            }
            return d <= JumpMax + 1e-6f ? Mark.Jump : Mark.Wall;
        }

        /// <summary>箱ごとの印。まわりにいちばん近い高さの面がある所から登ると考える。</summary>
        public static Mark[] Marks(IReadOnlyList<Box> boxes)
        {
            var result = new Mark[boxes.Count];
            for (int i = 0; i < boxes.Count; i++)
            {
                // 机の板（高さ0）はどの箱のまわりにもある
                var best = boxes[i].Height;
                for (int j = 0; j < boxes.Count; j++)
                {
                    if (i == j || !BoxColorAssigner.AreNeighbors(boxes[i].Footprint, boxes[j].Footprint, NeighborMargin))
                    {
                        continue;
                    }
                    best = Mathf.Min(best, Mathf.Abs(boxes[i].Height - boxes[j].Height));
                }
                result[i] = Classify(best);
            }
            return result;
        }
    }
}
