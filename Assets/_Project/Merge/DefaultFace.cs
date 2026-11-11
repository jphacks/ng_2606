using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 「いつもの こ」の顔の絵（2026-11-11）。写真の代わりに、立体の体（CharacterBody3D）の頭に貼る。
    /// 撮った顔の写真と同じ形（縦横の比 HumanMask.Aspect、顔の枠 HumanMask.Face）で描くので、写真のキャラと同じ貼り方で使える。
    /// 肌の色の上に、前髪・目（白い光つき）・ほっぺ・笑った口を描く。後ろ頭の色は HairColor。
    /// </summary>
    public static class DefaultFace
    {
        public const int Width = 256;
        public static int Height => Mathf.RoundToInt(Width / HumanMask.Aspect);

        public static readonly Color32 Skin = new Color32(255, 214, 178, 255);
        public static readonly Color HairColor = new Color(0.30f, 0.18f, 0.10f);
        static readonly Color32 Eye = new Color32(45, 35, 40, 255);
        static readonly Color32 Cheek = new Color32(255, 160, 160, 255);
        static readonly Color32 Mouth = new Color32(190, 70, 70, 255);

        // 顔の枠の中の位置（-1〜1。x は右が +、y は上が +）
        const float EyeX = 0.30f;
        const float EyeY = 0.02f;
        const float CheekX = 0.52f;
        const float CheekY = -0.22f;

        /// <summary>顔の絵の画素（左下から、行ごと）。</summary>
        public static Color32[] Pixels(int width, int height)
        {
            var hair = (Color32)HairColor;
            var f = HumanMask.Face;
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // 顔の枠の中の位置
                    var px = (((x + 0.5f) / width) - f.x) / f.z;
                    var py = (((y + 0.5f) / height) - f.y) / f.w;
                    pixels[y * width + x] = ColorAt(px, py, hair);
                }
            }
            return pixels;
        }

        static Color32 ColorAt(float x, float y, Color32 hair)
        {
            // 前髪：上の方。真ん中が少し下がる、ゆるい山の形
            var fringe = 0.42f - 0.10f * Mathf.Cos(x * 9f) * (1f - Mathf.Abs(x)) + 0.25f * x * x;
            if (y > fringe) return hair;
            // 横の髪：顔のふち（後ろ頭の髪とつながる）
            if (Mathf.Abs(x) > 0.74f && y > -0.2f + 0.6f * (1f - Mathf.Abs(x))) return hair;

            // 目：縦長の楕円。上の左寄りに、白い光
            foreach (var side in new[] { -1f, 1f })
            {
                var ex = (x - side * EyeX) / 0.075f;
                var ey = (y - EyeY) / 0.115f;
                if (ex * ex + ey * ey <= 1f)
                {
                    var hx = (x - side * EyeX + 0.025f) / 0.03f;
                    var hy = (y - EyeY - 0.045f) / 0.035f;
                    return hx * hx + hy * hy <= 1f ? new Color32(255, 255, 255, 255) : Eye;
                }
            }

            // 口：にっこりした弧
            var mx = x / 0.16f;
            if (Mathf.Abs(mx) <= 1f)
            {
                var arc = -0.24f - 0.07f * (1f - mx * mx);
                if (Mathf.Abs(y - arc) < 0.022f) return Mouth;
            }

            // ほっぺ：肌に、うすく赤みを混ぜる
            foreach (var side in new[] { -1f, 1f })
            {
                var cx = (x - side * CheekX) / 0.13f;
                var cy = (y - CheekY) / 0.08f;
                var d = cx * cx + cy * cy;
                if (d <= 1f) return Color32.Lerp(Skin, Cheek, 0.55f * (1f - d));
            }
            return Skin;
        }

        /// <summary>顔の絵の画像を作る。使い終わったら Destroy する。</summary>
        public static Texture2D Create()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "DefaultFace",
            };
            texture.SetPixels32(Pixels(Width, Height));
            texture.Apply();
            return texture;
        }
    }
}
