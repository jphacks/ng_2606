using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// しかけの絵（コードで描く小さな絵）。水の矢印、炎の地面、回復の地面、
    /// ジャンプ台の地面、敵の出る地面、敵の顔、ワープの地面。ファイルは持たない。
    /// 立ち上がる炎と＋は、立体の形（GimmickMeshes。2026-11-11）。
    /// </summary>
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

        /// <summary>ジャンプ台の地面：黄色に、上を指す白い山形（＾）を 2 つ。</summary>
        public static Texture2D JumpPad()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                    var chevron = false;
                    foreach (var offset in new[] { -0.12f, 0.14f })
                    {
                        var dy = p.y - offset;
                        chevron |= Mathf.Abs(dy + Mathf.Abs(p.x) * 0.9f) < 0.05f && Mathf.Abs(p.x) < 0.3f;
                    }
                    var glow = Mathf.Clamp01(1f - p.magnitude * 1.5f);
                    var baseColor = Color.Lerp(new Color(1f, 0.78f, 0.1f), new Color(1f, 0.95f, 0.45f), glow);
                    var color = chevron ? Color.white : baseColor;
                    pixels[y * Size + x] = C(color.r, color.g, color.b, chevron ? 0.98f : 0.85f);
                }
            }
            return New(pixels, TextureWrapMode.Repeat);
        }

        /// <summary>敵の出る地面：紫に、こい紫のうずまき。</summary>
        public static Texture2D EnemyPad()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                    var r = p.magnitude;
                    var angle = Mathf.Atan2(p.y, p.x);
                    var swirl = 0.5f + 0.5f * Mathf.Sin(r * 40f - angle * 2f);
                    var color = Color.Lerp(new Color(0.45f, 0.2f, 0.7f), new Color(0.7f, 0.45f, 0.95f), swirl * Mathf.Clamp01(1f - r * 1.4f));
                    pixels[y * Size + x] = C(color.r, color.g, color.b, 0.88f);
                }
            }
            return New(pixels, TextureWrapMode.Repeat);
        }

        /// <summary>敵の顔（紫の丸に、白い目と黒いひとみ。まわりは透明）。</summary>
        public static Texture2D EnemyFace()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                    var body = p.magnitude < 0.46f;
                    var eyeL = (p - new Vector2(-0.15f, 0.08f)).magnitude;
                    var eyeR = (p - new Vector2(0.15f, 0.08f)).magnitude;
                    Color color;
                    // 体は赤（2026-11-07：紫だと見えにくかったので）。目は白と黒、ふちは濃い赤で、机の上で目立たせる
                    if (eyeL < 0.05f || eyeR < 0.05f) color = new Color(0.1f, 0.05f, 0.05f);
                    else if (eyeL < 0.11f || eyeR < 0.11f) color = Color.white;
                    else if (p.magnitude > 0.42f) color = new Color(0.45f, 0.02f, 0.05f);
                    else color = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.8f, 0.05f, 0.08f), Mathf.Clamp01(p.magnitude * 2f));
                    pixels[y * Size + x] = C(color.r, color.g, color.b, body ? 1f : 0f);
                }
            }
            return New(pixels, TextureWrapMode.Clamp);
        }

        /// <summary>ワープの地面：ピンクに、白い輪が重なった模様。</summary>
        public static Texture2D Warp()
        {
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                    var r = p.magnitude;
                    var ring = Mathf.Abs(Mathf.Repeat(r * 6f, 1f) - 0.5f) < 0.12f && r < 0.45f;
                    var baseColor = Color.Lerp(new Color(1f, 0.45f, 0.72f), new Color(1f, 0.75f, 0.88f), Mathf.Clamp01(1f - r * 1.6f));
                    var color = ring ? Color.white : baseColor;
                    pixels[y * Size + x] = C(color.r, color.g, color.b, ring ? 0.95f : 0.85f);
                }
            }
            return New(pixels, TextureWrapMode.Repeat);
        }
    }
}
