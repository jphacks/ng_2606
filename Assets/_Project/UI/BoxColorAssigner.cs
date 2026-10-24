using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 箱ごとに色の番号を決める。隣どうし（上から見て接している・重なっている）は同じ色にしない。
    /// 計算だけの普通のクラス（Step 7）。色が足りないときだけ、隣と同じ色を許す。
    /// </summary>
    public static class BoxColorAssigner
    {
        /// <param name="footprints">箱を上から見た範囲（XZ）。並び順に色を決める</param>
        /// <param name="paletteCount">色の数</param>
        /// <param name="margin">この距離（m）以内なら隣とみなす</param>
        public static int[] Assign(IReadOnlyList<Rect> footprints, int paletteCount, float margin)
        {
            var result = new int[footprints.Count];
            if (paletteCount <= 0)
            {
                return result;
            }
            var used = new bool[paletteCount];
            for (int i = 0; i < footprints.Count; i++)
            {
                System.Array.Clear(used, 0, used.Length);
                for (int j = 0; j < i; j++)
                {
                    if (AreNeighbors(footprints[i], footprints[j], margin))
                    {
                        used[result[j]] = true;
                    }
                }
                // 前の箱から順にずらして選ぶと、離れた箱どうしも色がばらける
                var start = i % paletteCount;
                result[i] = start;
                for (int k = 0; k < paletteCount; k++)
                {
                    var c = (start + k) % paletteCount;
                    if (!used[c])
                    {
                        result[i] = c;
                        break;
                    }
                }
            }
            return result;
        }

        public static bool AreNeighbors(Rect a, Rect b, float margin)
        {
            return a.xMin - margin <= b.xMax && b.xMin - margin <= a.xMax
                && a.yMin - margin <= b.yMax && b.yMin - margin <= a.yMax;
        }
    }
}
