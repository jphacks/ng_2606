using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// たしかめ画面の段の印を決める。計算だけの普通のクラス（Step 10）。
    /// 机の板から登れる面を順にたどり、たどり着ける箱は登るときの段の高さで、たどり着けない箱は壁として判定する。
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

        /// <summary>
        /// 箱ごとの印。机の板（高さ0。どの箱のまわりにもある）から、登れる面を順にたどって決める。
        /// たどり着ける箱は、登るときの段（たどり着ける隣の面との差のうち、いちばん小さいもの）で Walk か Jump。
        /// たどり着けない箱は Wall（隣の箱との差が小さくても、その隣の箱にも登れなければ意味がないため）。
        /// </summary>
        public static Mark[] Marks(IReadOnlyList<Box> boxes)
        {
            // 机から、いちばん大きな段がいちばん小さくなる道順で、近い順に箱を決めていく（最短経路と同じ考え方）。
            // 決まった箱からだけ先へ進むので、「まだ登れていない隣の箱から歩いて来られる」と数えることはない。
            var n = boxes.Count;
            var cost = new float[n];
            var step = new float[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++)
            {
                step[i] = boxes[i].Height;
                cost[i] = Classify(step[i]) != Mark.Wall ? step[i] : float.PositiveInfinity;
            }
            while (true)
            {
                var next = -1;
                for (int i = 0; i < n; i++)
                {
                    if (!done[i] && !float.IsPositiveInfinity(cost[i]) && (next < 0 || cost[i] < cost[next]))
                    {
                        next = i;
                    }
                }
                if (next < 0)
                {
                    break;
                }
                done[next] = true;
                for (int k = 0; k < n; k++)
                {
                    if (done[k] || !BoxColorAssigner.AreNeighbors(boxes[next].Footprint, boxes[k].Footprint, NeighborMargin))
                    {
                        continue;
                    }
                    var d = Mathf.Abs(boxes[k].Height - boxes[next].Height);
                    if (Classify(d) == Mark.Wall)
                    {
                        continue;
                    }
                    var c = Mathf.Max(cost[next], d);
                    if (c < cost[k] || (Mathf.Approximately(c, cost[k]) && d < step[k]))
                    {
                        cost[k] = c;
                        step[k] = d;
                    }
                }
            }
            var result = new Mark[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = done[i] ? Classify(step[i]) : Mark.Wall;
            }
            return result;
        }
    }
}
