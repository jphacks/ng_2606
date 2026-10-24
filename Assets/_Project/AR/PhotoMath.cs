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

        /// <summary>
        /// WebCamTexture の映像を、画面の向きに合わせて起こすための、Graphics.Blit の scale と offset を返す。
        /// angle は WebCamTexture.videoRotationAngle、mirrored は videoVerticallyMirrored。
        /// 横画面に固定したアプリでは、angle は 0 か 180 になる。90 と 270 は扱わず、false を返す
        /// （そのときは補正せず、そのまま使う）。
        /// </summary>
        public static bool TryUprightTransform(int angle, bool mirrored, out Vector2 scale, out Vector2 offset)
        {
            var normalized = ((angle % 360) + 360) % 360;
            scale = Vector2.one;
            offset = Vector2.zero;
            if (normalized != 0 && normalized != 180)
            {
                return false;
            }

            // 180 度回すのは、左右と上下をどちらも反転すること。上下反転の印があれば、上下をもう一度反転する
            var sx = normalized == 180 ? -1f : 1f;
            var sy = (normalized == 180 ? -1f : 1f) * (mirrored ? -1f : 1f);
            scale = new Vector2(sx, sy);
            offset = new Vector2(sx < 0f ? 1f : 0f, sy < 0f ? 1f : 0f);
            return true;
        }
    }
}
