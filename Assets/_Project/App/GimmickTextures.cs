using UnityEngine;

namespace DeskStage.App
{
    /// <summary>しかけの絵（コードで描く小さな絵）。水の矢印、炎の地面、炎、回復の地面、＋。ファイルは持たない。</summary>
    public static class GimmickTextures
    {
        const int Size = 64;

        static Texture2D New(Color32[] pixels, TextureWrapMode wrap)
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
            };
            t.SetPixels32(pixels);
            t.Apply();
            return t;
        }

        static Color32 C(float r, float g, float b, float a) => new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), (byte)(Mathf.Clamp01(a) * 255f));

        /// <summary>水の板：青い水面に、流れる向き（direction）を指す、白い矢印。1 枚を繰り返して並べる（つなぎ目が見えない）。</summary>
        public static Texture2D Water(Vector2 direction)
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                    var along = Vector2.Dot(p, direction);
                    var across = Mathf.Abs(p.x * direction.y - p.y * direction.x);
                    // 水面のゆらぎ
                    var wave = 0.5f + 0.5f * Mathf.Sin((along + 0.5f) * Mathf.PI * 4f);
                    var baseColor = Color.Lerp(new Color(0.18f, 0.5f, 0.95f), new Color(0.35f, 0.7f, 1f), wave * 0.5f);
                    // 矢印（先が前を向いた「く」の字）
                    var arrow = Mathf.Abs(across - (0.22f - along)) < 0.05f && along > -0.2f && along < 0.26f && across < 0.3f;
                    var color = arrow ? Color.white : baseColor;
                    pixels[y * Size + x] = C(color.r, color.g, color.b, arrow ? 0.95f : 0.85f);
                }
            }
            return New(pixels, TextureWrapMode.Repeat);
        }

        /// <summary>炎の地面：赤とオレンジの、もやもやした模様（上へ流れる）。</summary>
        public static Texture2D Fire()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var u = (x + 0.5f) / Size;
                    var v = (y + 0.5f) / Size;
                    // 横にも縦にも つながる 波の重なり
                    var n = 0.5f + 0.25f * Mathf.Sin(2f * Mathf.PI * (u * 3f + v * 2f))
                                 + 0.25f * Mathf.Sin(2f * Mathf.PI * (u * 5f - v * 4f + 0.3f));
                    var color = Color.Lerp(new Color(0.75f, 0.1f, 0.05f), new Color(1f, 0.75f, 0.15f), Mathf.Clamp01(n));
                    pixels[y * Size + x] = C(color.r, color.g, color.b, 0.92f);
                }
            }
            return New(pixels, TextureWrapMode.Repeat);
        }

        /// <summary>炎（しずくの形。下が赤、上がオレンジ〜黄色）。</summary>
        public static Texture2D Flame()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var u = (x + 0.5f) / Size * 2f - 1f; // -1〜1
                    var v = (y + 0.5f) / Size;           // 0（下）〜1（上）
                    // 下は太く、上は細い。先がとがる
                    var width = Mathf.Pow(1f - v, 0.8f) * (0.25f + 0.65f * Mathf.Sin(Mathf.Clamp01(v * 3f) * Mathf.PI * 0.5f));
                    var inside = Mathf.Abs(u) < width;
                    var edge = Mathf.Clamp01((width - Mathf.Abs(u)) * 6f);
                    var color = Color.Lerp(new Color(1f, 0.25f, 0.05f), new Color(1f, 0.9f, 0.3f), Mathf.Clamp01(v * 1.3f));
                    // 中心は明るく
                    color = Color.Lerp(color, new Color(1f, 1f, 0.8f), Mathf.Clamp01(1f - Mathf.Abs(u) / Mathf.Max(width, 0.01f)) * 0.5f * (1f - v));
                    pixels[y * Size + x] = C(color.r, color.g, color.b, inside ? edge : 0f);
                }
            }
            return New(pixels, TextureWrapMode.Clamp);
        }

        /// <summary>回復の地面：やわらかい緑に、白い＋を、1 つ。</summary>
        public static Texture2D Heal()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                    var plus = (Mathf.Abs(p.x) < 0.06f && Mathf.Abs(p.y) < 0.24f) || (Mathf.Abs(p.y) < 0.06f && Mathf.Abs(p.x) < 0.24f);
                    var glow = Mathf.Clamp01(1f - p.magnitude * 1.6f);
                    var baseColor = Color.Lerp(new Color(0.25f, 0.78f, 0.45f), new Color(0.6f, 0.95f, 0.7f), glow);
                    var color = plus ? Color.white : baseColor;
                    pixels[y * Size + x] = C(color.r, color.g, color.b, plus ? 0.98f : 0.82f);
                }
            }
            return New(pixels, TextureWrapMode.Repeat);
        }

        /// <summary>ふわっと上がる＋（白い＋に、緑のふち。まわりは透明）。</summary>
        public static Texture2D Plus()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                    var core = (Mathf.Abs(p.x) < 0.09f && Mathf.Abs(p.y) < 0.36f) || (Mathf.Abs(p.y) < 0.09f && Mathf.Abs(p.x) < 0.36f);
                    var rim = (Mathf.Abs(p.x) < 0.14f && Mathf.Abs(p.y) < 0.41f) || (Mathf.Abs(p.y) < 0.14f && Mathf.Abs(p.x) < 0.41f);
                    var color = core ? Color.white : new Color(0.2f, 0.75f, 0.4f);
                    pixels[y * Size + x] = C(color.r, color.g, color.b, rim ? 1f : 0f);
                }
            }
            return New(pixels, TextureWrapMode.Clamp);
        }
    }
}
