using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 物が机に接するところの、やわらかい暗がり（2026-11-11。「ステージを現実に近づける」の光と影）。
    /// 本物の物の足もとには、光がまわりこまない、ぼんやりした暗がりができる。光の影（StageLighting）だけでは、
    /// 光の向きと反対側の足もとが明るいままで、箱が机から浮いて見える。
    /// 箱ごとに、底の形より少し大きな板を机の上に敷き、箱のふちから外へ、だんだん薄くなる暗さを描く。
    /// 画面全体で暗がりを計算する方法（SSAO）より、iPhone で軽い。
    /// 暗さの形の計算は、EditMode テストで確かめる（Alpha、Margin）。
    /// </summary>
    public static class StageContactShadows
    {
        public const string ShadowName = "ContactShadow";

        /// <summary>箱のふちのすぐ外の暗さ（0〜1）。</summary>
        public const float EdgeAlpha = 0.6f;

        /// <summary>暗がりの色（ほぼ黒。少し暖かい）。</summary>
        static readonly Color ShadowColor = new Color(0.08f, 0.06f, 0.05f);

        const int TextureSide = 64;

        // 机の面から、ほんの少し浮かせる（m。机と重なって、ちらつかないように）
        const float Lift = 0.0004f;

        /// <summary>暗がりが、箱のふちから外へ広がる幅（m）。背の高い物ほど広い。</summary>
        public static float Margin(float height) => Mathf.Clamp(height * 0.45f, 0.008f, 0.05f);

        /// <summary>
        /// 箱のふちからの距離 distance（m。箱の中は 0）での暗さ。ふちで EdgeAlpha、margin で 0。外へ向かって、なめらかに薄くなる。
        /// </summary>
        public static float Alpha(float distance, float margin)
        {
            if (margin <= 0f) return 0f;
            var t = Mathf.Clamp01(distance / margin);
            var fade = 1f - t * t * (3f - 2f * t); // なめらかに 1 → 0
            return EdgeAlpha * fade * fade;
        }

        /// <summary>
        /// ステージの箱（stage.boxes の id で、root の子を探す）の足もとに、暗がりを敷く。作った物は created と extras に入れる（片付けは呼ぶ側）。
        /// template は、URP の Unlit の素材（半透明に切り替えて使う）。
        /// </summary>
        public static void Add(Transform root, Shared.StageData stage, Material template, List<Object> created, List<GameObject> extras)
        {
            if (root == null || stage == null || stage.boxes == null || template == null) return;
            var boxes = new List<Transform>();
            foreach (var b in stage.boxes)
            {
                var child = root.Find(b.id);
                if (child == null) continue;
                if (child.localScale.x <= 0f || child.localScale.y <= 0f || child.localScale.z <= 0f) continue;
                boxes.Add(child);
            }
            foreach (var box in boxes)
            {
                var size = box.localScale;
                var margin = Margin(size.y);
                var texture = MakeTexture(size.x, size.z, margin);
                created.Add(texture);
                var material = new Material(template);
                MakeTransparent(material);
                material.SetTexture("_BaseMap", texture);
                material.mainTexture = texture;
                material.SetColor("_BaseColor", Color.white);
                material.color = Color.white;
                created.Add(material);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = ShadowName;
                var collider = quad.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
                }
                quad.transform.SetParent(root, false);
                var bottom = box.localPosition.y - size.y / 2f;
                quad.transform.localPosition = new Vector3(box.localPosition.x, bottom + Lift, box.localPosition.z);
                quad.transform.localRotation = box.localRotation * Quaternion.Euler(90f, 0f, 0f);
                quad.transform.localScale = new Vector3(size.x + margin * 2f, size.z + margin * 2f, 1f);
                quad.layer = box.gameObject.layer;
                var renderer = quad.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                extras.Add(quad);
            }
        }

        // 底の形（幅 width、奥行き depth）のまわりに margin だけ広げた板の絵。箱のふちからの距離で、暗さを決める
        static Texture2D MakeTexture(float width, float depth, float margin)
        {
            var fullW = width + margin * 2f;
            var fullD = depth + margin * 2f;
            var pixels = new Color32[TextureSide * TextureSide];
            for (var y = 0; y < TextureSide; y++)
            {
                var z = ((y + 0.5f) / TextureSide - 0.5f) * fullD;
                for (var x = 0; x < TextureSide; x++)
                {
                    var px = ((x + 0.5f) / TextureSide - 0.5f) * fullW;
                    var dx = Mathf.Max(Mathf.Abs(px) - width / 2f, 0f);
                    var dz = Mathf.Max(Mathf.Abs(z) - depth / 2f, 0f);
                    var c = ShadowColor;
                    c.a = Alpha(Mathf.Sqrt(dx * dx + dz * dz), margin);
                    pixels[y * TextureSide + x] = c;
                }
            }
            var texture = new Texture2D(TextureSide, TextureSide, TextureFormat.RGBA32, false)
            {
                name = ShadowName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        // URP の Unlit の素材を、半透明（ふつうの重ね方）にする。机の枠やスキャン中の箱の素材と同じ設定
        static void MakeTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10; // ほかの半透明（しかけの模様など）より先に描く
        }
    }
}
