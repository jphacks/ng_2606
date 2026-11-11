using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 炎と回復のエフェクトの、立体の形と色（2026-11-11。前は、カメラへ向けた板の絵で、横から見るとうすかった）。
    /// しかけの素材は光の計算をしない（Unlit）ので、陰は色の絵に描き込む：
    ///  ・炎（Flame）：しずくの形の回転体。u は「カメラから見た正面からの角度」（0 が正面、1 が真後ろ）、v は高さ。
    ///    キャラの横の向きだけカメラへ向けて使い、正面を明るく、ふちを暗く塗ると、どこから見てもふくらんで見える
    ///  ・回復（Plus）：厚みのある「＋」。面の向きごとに、色の絵の別の場所を使う（上と正面は明るく、側面は濃い）
    ///  ・火の粉（Spark）：小さな八面体
    /// どれも、大きさ 1（炎は、幅 1・高さ 1。足もとが原点）。
    /// </summary>
    public static class GimmickMeshes
    {
        const int FlameSides = 18;
        const int FlameRings = 14;

        /// <summary>炎の太さ（高さ 0〜1 の位置で、半径 0〜0.5）。下は丸く、いちばん太いのは下の 1/3、上はとがる。</summary>
        public static float FlameRadius(float y)
        {
            y = Mathf.Clamp01(y);
            return 0.5f * Mathf.Sin(Mathf.PI * Mathf.Pow(y, 0.6f));
        }

        public static Mesh Flame()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (var r = 0; r <= FlameRings; r++)
            {
                var y = r / (float)FlameRings;
                var radius = FlameRadius(y);
                for (var s = 0; s <= FlameSides; s++)
                {
                    // 角度 0 は、カメラの方（-z）
                    var angle = 2f * Mathf.PI * s / FlameSides;
                    vertices.Add(new Vector3(Mathf.Sin(angle) * radius, y, -Mathf.Cos(angle) * radius));
                    uvs.Add(new Vector2((1f - Mathf.Cos(angle)) * 0.5f, y));
                }
            }
            for (var r = 0; r < FlameRings; r++)
            {
                for (var s = 0; s < FlameSides; s++)
                {
                    var i0 = r * (FlameSides + 1) + s;
                    var i1 = i0 + 1;
                    var i2 = i0 + FlameSides + 1;
                    var i3 = i2 + 1;
                    triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
                    triangles.Add(i1); triangles.Add(i2); triangles.Add(i3);
                }
            }
            var mesh = new Mesh { name = "Flame" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 炎の色の絵。下（v=0）から上へ：白っぽい黄色 → 黄色 → オレンジ → 赤。正面（u=0）が明るく、ふち（u=0.5）から後ろは暗い。
        /// core が true なら、内側の芯の炎（全体に明るく、白っぽい）。上の先は、うすくして消える。
        /// </summary>
        public static Texture2D FlameTexture(bool core)
        {
            const int w = 64;
            const int h = 64;
            var pixels = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                var v = (y + 0.5f) / h;
                var color = core ? CoreGradient(v) : FlameGradient(v);
                for (var x = 0; x < w; x++)
                {
                    var u = (x + 0.5f) / w;
                    // 正面からの角度の cos（1：正面、0：ふち、-1：真後ろ）
                    var facing = 1f - 2f * u;
                    var shade = core ? Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(facing)) : Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(facing * 1.2f));
                    var c = color * shade;
                    c.a = Mathf.Clamp01((1f - v) / 0.18f) * (core ? 0.95f : 0.92f);
                    pixels[y * w + x] = c;
                }
            }
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = core ? "FlameCore" : "Flame",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        static Color FlameGradient(float v)
        {
            var white = new Color(1f, 0.97f, 0.75f);
            var yellow = new Color(1f, 0.84f, 0.2f);
            var orange = new Color(1f, 0.5f, 0.08f);
            var red = new Color(0.9f, 0.18f, 0.06f);
            if (v < 0.2f) return Color.Lerp(white, yellow, v / 0.2f);
            if (v < 0.55f) return Color.Lerp(yellow, orange, (v - 0.2f) / 0.35f);
            return Color.Lerp(orange, red, (v - 0.55f) / 0.45f);
        }

        static Color CoreGradient(float v)
        {
            return Color.Lerp(new Color(1f, 1f, 0.9f), new Color(1f, 0.85f, 0.3f), v);
        }

        /// <summary>
        /// 厚みのある「＋」。大きさ 1（腕の端から端まで）、腕の太さ ArmWidth、厚み Thickness。真ん中が原点。
        /// 色の絵（PlusTexture）の、正面・後ろ（u 0〜0.25）、上（0.25〜0.5）、横（0.5〜0.75）、下（0.75〜1）を使う。
        /// </summary>
        public const float ArmWidth = 0.36f;
        public const float Thickness = 0.3f;

        public static Mesh Plus()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            AddBox(vertices, uvs, triangles, new Vector3(1f, ArmWidth, Thickness));
            AddBox(vertices, uvs, triangles, new Vector3(ArmWidth, 1f, Thickness));
            var mesh = new Mesh { name = "Plus" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // 箱（大きさ size、真ん中が原点）を足す。面ごとに、向きに合った色の場所を使う
        static void AddBox(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, Vector3 size)
        {
            var h = size * 0.5f;
            var faces = new[]
            {
                (n: Vector3.back, u: 0.125f),
                (n: Vector3.forward, u: 0.125f),
                (n: Vector3.up, u: 0.375f),
                (n: Vector3.right, u: 0.625f),
                (n: Vector3.left, u: 0.625f),
                (n: Vector3.down, u: 0.875f),
            };
            foreach (var (n, u) in faces)
            {
                // 面の 2 つの軸
                var a = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                var b = Vector3.Cross(n, a);
                var start = vertices.Count;
                var center = Vector3.Scale(n, h);
                var ha = Vector3.Scale(a, h);
                var hb = Vector3.Scale(b, h);
                vertices.Add(center - ha - hb);
                vertices.Add(center + ha - hb);
                vertices.Add(center + ha + hb);
                vertices.Add(center - ha + hb);
                for (var i = 0; i < 4; i++) uvs.Add(new Vector2(u, 0.5f));
                triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            }
        }

        /// <summary>「＋」の色の絵（横 4 つの区切り：正面・後ろ、上、横、下）。上は白っぽい緑、正面は明るい緑、横と下は濃い緑。</summary>
        public static Texture2D PlusTexture()
        {
            var colors = new[]
            {
                new Color32(150, 240, 170, 255), // 正面・後ろ
                new Color32(200, 255, 210, 255), // 上（光が当たる）
                new Color32(35, 165, 85, 255),   // 横
                new Color32(20, 115, 60, 255),   // 下
            };
            const int w = 16;
            var pixels = new Color32[w * 4];
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < w; x++) pixels[y * w + x] = colors[x / 4];
            }
            var texture = new Texture2D(w, 4, TextureFormat.RGBA32, false)
            {
                name = "Plus3D",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>火の粉：小さな八面体（大きさ 1）。色の絵は、炎の絵の下の明るい所を使う。</summary>
        public static Mesh Spark()
        {
            var points = new[]
            {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back,
            };
            var faces = new[]
            {
                (0, 4, 3), (0, 3, 5), (0, 5, 2), (0, 2, 4),
                (1, 3, 4), (1, 5, 3), (1, 2, 5), (1, 4, 2),
            };
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            foreach (var (a, b, c) in faces)
            {
                foreach (var i in new[] { a, b, c })
                {
                    triangles.Add(vertices.Count);
                    vertices.Add(points[i] * 0.5f);
                    uvs.Add(new Vector2(0.05f, 0.3f));
                }
            }
            var mesh = new Mesh { name = "Spark" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
