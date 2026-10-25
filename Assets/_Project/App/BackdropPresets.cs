using System;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// きれいな背景のプリセット（コードで描く絵）。写真を撮らなくても、ステージの うしろに出す けしきを えらべる。
    /// 絵は、横にぐるっと つないだとき（背景は、ステージを囲む壁）、つなぎ目が目立たないよう、横向きに繰り返せる形で描く。
    /// 画素の並びは、添字 0 が左下の行（Texture2D.SetPixels32 に渡せる）。同じ id・大きさなら、いつも同じ絵になる。
    /// </summary>
    public static class BackdropPresets
    {
        public struct Preset
        {
            public string Id;
            public string Name;
        }

        public static readonly Preset[] All =
        {
            new Preset { Id = "sky", Name = "あおぞら" },
            new Preset { Id = "sunset", Name = "ゆうやけ" },
            new Preset { Id = "forest", Name = "もり" },
            new Preset { Id = "sea", Name = "うみ" },
            new Preset { Id = "night", Name = "ほしぞら" },
            new Preset { Id = "room", Name = "おへや" },
        };

        public static bool IsKnown(string id)
        {
            foreach (var p in All)
            {
                if (p.Id == id) return true;
            }
            return false;
        }

        /// <summary>絵を描く。知らない id なら null。</summary>
        public static Color32[] Render(string id, int width, int height)
        {
            if (width <= 0 || height <= 0 || !IsKnown(id))
            {
                return null;
            }
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var v = (y + 0.5f) / height; // 0 が下、1 が上
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / width;
                    pixels[y * width + x] = (Color32)Pixel(id, u, v);
                }
            }
            return pixels;
        }

        static Color Pixel(string id, float u, float v)
        {
            switch (id)
            {
                case "sky": return Sky(u, v);
                case "sunset": return Sunset(u, v);
                case "forest": return Forest(u, v);
                case "sea": return Sea(u, v);
                case "night": return Night(u, v);
                default: return Room(u, v);
            }
        }

        // ---- 絵 ----

        static Color Sky(float u, float v)
        {
            var c = Color.Lerp(Hex(0xCFEBFF), Hex(0x5AAEF0), Mathf.Pow(v, 0.8f));
            var cloud = Mathf.SmoothStep(0.52f, 0.78f, Fbm(u, v * 0.9f, 6, 3, 11)) * Mathf.Lerp(1f, 0.55f, v);
            return Color.Lerp(c, Color.white, cloud * 0.9f);
        }

        static Color Sunset(float u, float v)
        {
            var c = v < 0.35f
                ? Color.Lerp(Hex(0xFFD08A), Hex(0xFF8A7A), v / 0.35f)
                : Color.Lerp(Hex(0xFF8A7A), Hex(0x6E5BB5), Mathf.Clamp01((v - 0.35f) / 0.65f));
            var du = Mathf.Min(Mathf.Abs(u - 0.5f), 1f - Mathf.Abs(u - 0.5f));
            var d = Mathf.Sqrt(du * du * 4f + (v - 0.3f) * (v - 0.3f));
            c = Color.Lerp(c, Hex(0xFFF1B8), Mathf.Clamp01(1f - d / 0.45f) * 0.8f);
            var cloud = Mathf.SmoothStep(0.6f, 0.82f, Fbm(u, v * 1.4f, 5, 3, 23)) * Mathf.Clamp01((v - 0.3f) * 3f);
            return Color.Lerp(c, Hex(0xFFC9B0), cloud * 0.55f);
        }

        static Color Forest(float u, float v)
        {
            var c = Color.Lerp(Hex(0xF4F9E4), Hex(0xA9DDF5), Mathf.Clamp01(v * 1.2f));
            // 3 まいの 丘（遠いほど うすく、高い）
            var layers = new[]
            {
                new { Base = 0.50f, Amp = 0.07f, Freq = 2f, Phase = 0.1f, Color = Hex(0x9ED7A5) },
                new { Base = 0.36f, Amp = 0.08f, Freq = 3f, Phase = 0.55f, Color = Hex(0x6FC48A) },
                new { Base = 0.22f, Amp = 0.07f, Freq = 4f, Phase = 0.3f, Color = Hex(0x3FA56C) },
            };
            foreach (var l in layers)
            {
                var top = l.Base + l.Amp * Mathf.Sin(2f * Mathf.PI * (u * l.Freq + l.Phase))
                          + l.Amp * 0.4f * Mathf.Sin(2f * Mathf.PI * (u * l.Freq * 2f + l.Phase * 3f));
                if (v < top)
                {
                    c = Color.Lerp(l.Color, Color.Lerp(l.Color, Color.black, 0.18f), Mathf.Clamp01((top - v) * 2.5f));
                }
            }
            return c;
        }

        static Color Sea(float u, float v)
        {
            const float horizon = 0.5f;
            if (v >= horizon)
            {
                var t = (v - horizon) / (1f - horizon);
                var c = Color.Lerp(Hex(0xE3F4FF), Hex(0x6BB7F0), Mathf.Pow(t, 0.7f));
                var cloud = Mathf.SmoothStep(0.58f, 0.8f, Fbm(u, v * 1.2f, 6, 3, 31)) * Mathf.Clamp01(t * 2f);
                return Color.Lerp(c, Color.white, cloud * 0.8f);
            }
            var s = v / horizon; // 0 が下、1 が水平線
            var sea = Color.Lerp(Hex(0x1F8FC9), Hex(0x7FD6E8), Mathf.Pow(s, 1.4f));
            var wave = Mathf.Sin(2f * Mathf.PI * (u * 18f + s * s * 6f)) * 0.5f + 0.5f;
            wave = Mathf.SmoothStep(0.8f, 1f, wave) * Mathf.Lerp(0.5f, 0.15f, s);
            return Color.Lerp(sea, Color.white, wave);
        }

        static Color Night(float u, float v)
        {
            var c = Color.Lerp(Hex(0x2B3A75), Hex(0x0B1033), Mathf.Pow(v, 0.7f));
            // 星（細かい格子ごとに、ときどき 光る）
            const int cellsX = 220, cellsY = 110;
            var cx = Mathf.FloorToInt(u * cellsX);
            var cy = Mathf.FloorToInt(v * cellsY);
            var h = Hash(cx, cy, 7);
            if (h > 0.965f)
            {
                var fx = u * cellsX - cx - 0.5f;
                var fy = v * cellsY - cy - 0.5f;
                var d = Mathf.Sqrt(fx * fx + fy * fy);
                var star = Mathf.Clamp01(1f - d * 2.2f) * Mathf.Lerp(0.5f, 1f, (h - 0.965f) / 0.035f);
                c = Color.Lerp(c, Hex(0xFFF6D6), star);
            }
            // 月
            var mu = Mathf.Min(Mathf.Abs(u - 0.7f), 1f - Mathf.Abs(u - 0.7f));
            var md = Mathf.Sqrt(mu * mu * 4f + (v - 0.68f) * (v - 0.68f));
            c = Color.Lerp(c, Hex(0xFFF3C4), Mathf.Clamp01(1f - md / 0.25f) * 0.25f);
            c = Color.Lerp(c, Hex(0xFFF8DE), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.075f - md) / 0.01f)));
            return c;
        }

        static Color Room(float u, float v)
        {
            const float floorTop = 0.2f;
            if (v < floorTop)
            {
                var plank = Mathf.Floor(u * 14f);
                var shade = 0.92f + 0.08f * Hash((int)plank, 0, 5);
                var seam = Mathf.SmoothStep(0.92f, 1f, Mathf.Abs(Mathf.Sin(Mathf.PI * u * 14f)));
                var wood = Color.Lerp(Hex(0xE5BE8C), Hex(0xD3A56E), Mathf.Clamp01(v / floorTop)) * shade;
                wood.a = 1f;
                return Color.Lerp(wood, Hex(0xB98852), seam * 0.5f);
            }
            var wall = Color.Lerp(Hex(0xFFF0DC), Hex(0xFFE3C4), (v - floorTop));
            // やわらかい水玉
            const float dots = 16f;
            var gx = u * dots;
            var gy = (v - floorTop) * dots * 0.5f;
            var offset = (Mathf.FloorToInt(gy) % 2) * 0.5f;
            var fx = Mathf.Repeat(gx + offset, 1f) - 0.5f;
            var fy = Mathf.Repeat(gy, 1f) - 0.5f;
            var dot = Mathf.SmoothStep(0.2f, 0.16f, Mathf.Sqrt(fx * fx + fy * fy));
            wall = Color.Lerp(wall, Hex(0xFFC9D2), dot * 0.7f);
            // 床との境の、巾木
            var baseboard = Mathf.SmoothStep(floorTop + 0.035f, floorTop + 0.03f, v);
            return Color.Lerp(wall, Hex(0xFFFFFF), baseboard);
        }

        // ---- 道具 ----

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        // 格子ごとの、決まった乱数（0〜1）
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        // 横に つながる 値のノイズ（u の 0 と 1 が、同じ値になる）
        static float Noise(float u, float v, int cellsX, int cellsY, int seed)
        {
            var x = u * cellsX;
            var y = v * cellsY;
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var fx = x - x0;
            var fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float At(int ix, int iy) => Hash(((ix % cellsX) + cellsX) % cellsX, iy, seed);
            var a = Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), fx);
            var b = Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), fx);
            return Mathf.Lerp(a, b, fy);
        }

        static float Fbm(float u, float v, int cellsX, int cellsY, int seed)
        {
            var sum = 0f;
            var amp = 0.5f;
            var total = 0f;
            for (var i = 0; i < 4; i++)
            {
                sum += amp * Noise(u, v, cellsX << i, cellsY << i, seed + i);
                total += amp;
                amp *= 0.5f;
            }
            return sum / total;
        }
    }
}
