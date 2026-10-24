using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>写真の大きさの計算（A の Step 22）。EditMode テストで確かめる。</summary>
    public static class PhotoMath
    {
        /// <summary>
        /// 長い辺が maxLongSide 以下になるように、縦横の比を保って縮めた大きさを返す。
        /// もともと小さければそのまま。どちらの辺も 1 より小さくしない。
        /// </summary>
        public static Vector2Int FitLongSide(int width, int height, int maxLongSide)
        {
            var longSide = Mathf.Max(width, height);
            if (longSide <= maxLongSide || longSide <= 0)
            {
                return new Vector2Int(width, height);
            }
            var scale = (float)maxLongSide / longSide;
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(height * scale)));
        }
    }
}
