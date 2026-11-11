using UnityEngine;
using UnityEngine.Rendering;

namespace DeskStage.Merge
{
    /// <summary>
    /// 遊ぶステージの光を整える（2026-11-11。「ステージを現実に近づける」の 1 つ目：光と影）。StagePhotoTexturer が、写真と背景を貼ったあとに呼ぶ。
    ///  ・光源は 1 つ：App_System の PlayLight（影を落とす）だけを使い、ほかの向きのある光（Main.unity の影なしの Directional Light）は消す。
    ///    2 つ重なって明るすぎ、写真の色が白っぽく飛んでいた
    ///  ・環境の光：背景の絵の、上・真ん中・下の色から、空・地平線・地面の 3 色を作る（影の中も、背景と同じ色味になる）
    ///  ・光の色：背景の絵の平均の色に、少し寄せる（ゆうやけなら、少しオレンジ）
    /// 写真の面は、光の計算をする素材（Lit）なので、光が直接当たる上の面が明るくなりすぎないよう、光の強さを抑える。
    /// 色の計算は、EditMode テストで確かめる（AmbientColors、LightColor）。
    /// </summary>
    public static class StageLighting
    {
        public const string PlayLightName = "PlayLight";

        /// <summary>影を落とす光の強さ。写真は本物の光で撮れているので、上の面が白く飛ばない強さにする。</summary>
        public const float SunIntensity = 1.0f;

        /// <summary>
        /// 光の向き：遊ぶカメラ（机の手前から奥を見る）から見て、左ななめ前の上から。影が右ななめ奥へ伸びて見え、
        /// 正面と左の面の明るさに差がつく（前は、カメラの後ろから当てていたので、影が箱の向こうに隠れ、正面がみな同じ明るさだった）。
        /// </summary>
        public static readonly Quaternion SunRotation = Quaternion.Euler(50f, 50f, 0f);

        /// <summary>影の濃さ（0〜1）。環境の光で影の中も見えるので、濃いめにする。</summary>
        public const float ShadowStrength = 0.9f;

        /// <summary>環境の光の強さ（背景の色に掛ける）。影の中が真っ暗にならず、光の当たる面との違いも分かる強さ。</summary>
        public const float AmbientStrength = 0.62f;

        /// <summary>背景の色に寄せる割合（0：白い光、1：背景の色そのもの）。</summary>
        public const float TintAmount = 0.2f;

        // 背景が無いときの環境の光（やわらかい昼の部屋）
        static readonly Color DefaultSky = new Color(0.52f, 0.56f, 0.62f);
        static readonly Color DefaultEquator = new Color(0.46f, 0.45f, 0.43f);
        static readonly Color DefaultGround = new Color(0.30f, 0.27f, 0.24f);
        static readonly Color WarmWhite = new Color(1f, 0.97f, 0.92f);

        /// <summary>
        /// 背景の絵（左下から行ごと）から、環境の光の 3 色（空・地平線・地面）。絵が無ければ、決まった色。
        /// 背景の色をそのまま使うと色が強すぎるので、灰色と半分ずつ混ぜて、AmbientStrength を掛ける。
        /// </summary>
        public static void AmbientColors(Color32[] pixels, int width, int height, out Color sky, out Color equator, out Color ground)
        {
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length < width * height)
            {
                sky = DefaultSky;
                equator = DefaultEquator;
                ground = DefaultGround;
                return;
            }
            var top = RowsAverage(pixels, width, height, 0.75f, 1f);
            var middle = RowsAverage(pixels, width, height, 0.35f, 0.65f);
            var bottom = RowsAverage(pixels, width, height, 0f, 0.2f);
            sky = Soften(top) * AmbientStrength * 1.1f;
            equator = Soften(middle) * AmbientStrength;
            ground = Soften(bottom) * AmbientStrength * 0.6f;
            sky.a = equator.a = ground.a = 1f;
        }

        /// <summary>影を落とす光の色：少し暖かい白を、背景の平均の色の明るさをそろえた色へ、TintAmount だけ寄せる。</summary>
        public static Color LightColor(Color32[] pixels, int width, int height)
        {
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length < width * height)
            {
                return WarmWhite;
            }
            var average = RowsAverage(pixels, width, height, 0f, 1f);
            var max = Mathf.Max(average.r, Mathf.Max(average.g, average.b));
            if (max < 1e-3f) return WarmWhite;
            var hue = new Color(average.r / max, average.g / max, average.b / max, 1f);
            var color = Color.Lerp(WarmWhite, hue, TintAmount);
            color.a = 1f;
            return color;
        }

        /// <summary>
        /// 光を整える。PlayLight の色と強さを変え、ほかの向きのある光を消し、環境の光を 3 色にする。
        /// pixels は背景の絵（無ければ null）。
        /// </summary>
        public static void Apply(Color32[] pixels, int width, int height)
        {
            Light sun = null;
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional && light.name == PlayLightName)
                {
                    sun = light;
                }
            }
            foreach (var light in lights)
            {
                if (light.type != LightType.Directional) continue;
                if (sun == null)
                {
                    sun = light; // PlayLight が無ければ、最初の光を使う
                    continue;
                }
                if (light != sun) light.enabled = false;
            }
            if (sun != null)
            {
                sun.color = LightColor(pixels, width, height);
                sun.intensity = SunIntensity;
                sun.transform.rotation = SunRotation;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = ShadowStrength;
            }

            AmbientColors(pixels, width, height, out var sky, out var equator, out var ground);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
        }

        // 背景の色を、灰色と半分ずつ混ぜる（環境の光が、背景の色に染まりすぎないように）
        static Color Soften(Color c)
        {
            var gray = (c.r + c.g + c.b) / 3f;
            return Color.Lerp(new Color(gray, gray, gray, 1f), c, 0.5f);
        }

        // from〜to（0：下、1：上）の行の、色の平均
        static Color RowsAverage(Color32[] pixels, int width, int height, float from, float to)
        {
            var y0 = Mathf.Clamp(Mathf.FloorToInt(from * height), 0, height - 1);
            var y1 = Mathf.Clamp(Mathf.CeilToInt(to * height), y0 + 1, height);
            var sum = Vector3.zero;
            var count = 0;
            for (var y = y0; y < y1; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    Color c = pixels[y * width + x];
                    sum += new Vector3(c.r, c.g, c.b);
                    count++;
                }
            }
            sum /= Mathf.Max(1, count);
            return new Color(sum.x, sum.y, sum.z, 1f);
        }
    }
}
