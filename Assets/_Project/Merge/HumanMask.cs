using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// キャラクターを写真で作るときの枠：顔だけの、まるい枠（2026-11-10。前は、頭・胴・両腕・両足の人型だった）。
    /// 顔を、この枠に合わせて撮り、枠の中だけを使う。立体のキャラ（CharacterBody3D）の頭の前に貼る。胴・手足は、だれが撮っても同じ。
    /// 座標は、枠の箱の中の割合：横は左から右へ 0〜1、縦は下から上へ 0〜1。箱の横：縦 ＝ Aspect。
    /// MonoBehaviour から切り離して、EditMode テストで確かめる。
    /// </summary>
    public static class HumanMask
    {
        /// <summary>
        /// 枠の箱の、横 ÷ 縦。4:5。前の人型の枠（3:4）で撮ったキャラと、撮った絵の縦横の比で見分ける（CharacterAvatar）。
        /// </summary>
        public const float Aspect = 0.8f;

        /// <summary>前の人型の枠の、横 ÷ 縦（このキャラは、人型の頭の部分を顔として使う）。</summary>
        public const float LegacyAspect = 0.75f;

        /// <summary>顔の楕円（x, y：中心、z, w：横と縦の半径。枠の箱の中の割合）。少し縦に長い。</summary>
        public static readonly Vector4 Face = new Vector4(0.50f, 0.50f, 0.46f, 0.40f);

        /// <summary>前の人型の枠の、頭の楕円（前の枠で撮ったキャラの顔として使う）。</summary>
        public static readonly Vector4 LegacyHead = new Vector4(0.50f, 0.80f, 0.27f, 0.19f);

        static readonly Vector4[] Parts = { Face };

        // ふちのぼかしの幅（楕円の大きさに対する割合）。小さいほど、くっきりする
        const float Edge = 0.06f;

        /// <summary>枠の箱の中の点 (x, y) が、枠の中か。0（外）〜1（中）。ふちは、少しぼかす。</summary>
        public static float Coverage(float x, float y) => Coverage(x, y, 0f);

        /// <summary>
        /// Coverage の、枠を grow だけ広げる版（0.1 なら、各部分の半径を 1.1 倍）。
        /// 背景との差で人形を取り出すとき、枠のふちで人形が欠けないよう、少し広い枠で区切る。
        /// </summary>
        public static float Coverage(float x, float y, float grow)
        {
            var best = float.MaxValue;
            var scale = 1f + grow;
            foreach (var p in Parts)
            {
                var dx = (x - p.x) / (p.z * scale);
                var dy = (y - p.y) / (p.w * scale);
                var f = Mathf.Sqrt(dx * dx + dy * dy);
                if (f < best) best = f;
            }
            return Mathf.Clamp01((1f - best) / Edge);
        }

        /// <summary>
        /// 写真（rgb、width x height。添字 0 が左下の行）に、枠の形の透明度を付ける。枠の外は透明（アルファ 0）、中は不透明。
        /// ふちは、半透明になる（素材の「アルファの切り捨て」で、0.5 を境に切れる）。
        /// </summary>
        public static Color32[] Apply(Color32[] rgb, int width, int height)
        {
            var result = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var v = (y + 0.5f) / height;
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / width;
                    var c = rgb[y * width + x];
                    result[y * width + x] = new Color32(c.r, c.g, c.b, (byte)Mathf.RoundToInt(Coverage(u, v) * 255f));
                }
            }
            return result;
        }

        /// <summary>
        /// カメラの映像（横 ÷ 縦 ＝ previewAspect）の中の、枠を置く場所。映像に対する割合（x、y は左下から。y は下から上へ）。
        /// 中央に、高さの約 84% で置く。映像が縦に細長いときは、横に収まるように小さくする。
        /// </summary>
        public static Rect CropRegion(float previewAspect)
        {
            var aspect = Mathf.Max(previewAspect, 0.1f);
            var h = 0.84f;
            var w = h * Aspect / aspect; // 映像の横に対する割合
            if (w > 0.92f)
            {
                var shrink = 0.92f / w;
                w *= shrink;
                h *= shrink;
            }
            return new Rect((1f - w) / 2f, (1f - h) / 2f, w, h);
        }

        /// <summary>
        /// カメラの映像の上に重ねる、枠の案内の絵（width x height。映像と同じ縦横の比）。
        /// 枠の外は暗く、枠のふちは白い線、枠の中は透明。
        /// </summary>
        public static Color32[] GuideOverlay(int width, int height, Rect region)
        {
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var v = (y + 0.5f) / height;
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / width;
                    float dim;
                    float line = 0f;
                    if (u < region.xMin || u > region.xMax || v < region.yMin || v > region.yMax)
                    {
                        dim = 0.65f;
                    }
                    else
                    {
                        var c = Coverage((u - region.xMin) / region.width, (v - region.yMin) / region.height);
                        dim = 0.65f * (1f - c);
                        line = Mathf.Clamp01(1f - Mathf.Abs(c - 0.5f) / 0.3f);
                    }
                    var gray = (byte)Mathf.RoundToInt(255f * line);
                    var alpha = (byte)Mathf.RoundToInt(255f * Mathf.Max(dim, line * 0.95f));
                    pixels[y * width + x] = new Color32(gray, gray, gray, alpha);
                }
            }
            return pixels;
        }

        /// <summary>枠の中に入る割合（箱に対する面積の割合）。枠が太めであることの確認用。</summary>
        public static float AreaFraction(int samples = 200)
        {
            var inside = 0;
            for (var y = 0; y < samples; y++)
            {
                for (var x = 0; x < samples; x++)
                {
                    if (Coverage((x + 0.5f) / samples, (y + 0.5f) / samples) >= 0.5f) inside++;
                }
            }
            return inside / (float)(samples * samples);
        }
    }
}
