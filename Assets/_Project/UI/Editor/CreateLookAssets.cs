using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeskStage.UI.Editor
{
    /// <summary>
    /// メニュー「DeskStage/D/見た目の素材を作る」（Step 7）。
    /// Rendering/ に、枠の画像、6つの素材、素材の組（ViewLook）を作る。
    /// もうある素材は作り直さない（人が Inspector で直した色を消さない）。
    /// </summary>
    public static class CreateLookAssets
    {
        const string Folder = "Assets/_Project/Rendering";
        const string TextureFolder = Folder + "/Textures";
        public const string ViewLookPath = Folder + "/ViewLook.asset";

        [MenuItem("DeskStage/D/見た目の素材を作る")]
        public static void Run()
        {
            Directory.CreateDirectory(TextureFolder);

            // 枠の画像：外側だけ色があり、中は透明（または別の色）
            var outlineTex = BorderTexture("Border_White", Color.white, new Color(1f, 1f, 1f, 0f), 6);
            var selectedTex = BorderTexture("Border_Yellow", new Color(1f, 0.85f, 0.1f), new Color(1f, 0.85f, 0.1f, 0.15f), 14);
            var deskTex = BorderTexture("Border_Desk", new Color(0.55f, 0.40f, 0.26f), new Color(0.78f, 0.62f, 0.44f), 8);

            var look = AssetDatabase.LoadAssetAtPath<ViewLook>(ViewLookPath);
            if (look == null)
            {
                look = ScriptableObject.CreateInstance<ViewLook>();
                AssetDatabase.CreateAsset(look, ViewLookPath);
            }

            look.ScanSurface = Material("ScanSurface", true, true, new Color(0.55f, 0.88f, 1f, 0.35f), null);
            look.DeskOutline = Material("DeskOutline", true, true, new Color(0.4f, 0.9f, 1f, 1f), outlineTex);
            look.BoxScan = Material("BoxScan", true, true, new Color(0.45f, 0.75f, 1f, 0.6f), null);
            look.BoxPlay = Material("BoxPlay", false, false, Color.white, null);
            look.DeskPlay = Material("DeskPlay", false, false, Color.white, deskTex);
            look.BoxSelected = Material("BoxSelected", true, true, Color.white, selectedTex);

            EditorUtility.SetDirty(look);
            AssetDatabase.SaveAssets();
            Debug.Log("見た目の素材を作りました: " + Folder);
        }

        static Texture2D BorderTexture(string name, Color border, Color inside, int width)
        {
            var path = TextureFolder + "/" + name + ".png";
            if (!File.Exists(path))
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var edge = x < width || y < width || x >= size - width || y >= size - width;
                        tex.SetPixel(x, y, edge ? border : inside);
                    }
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Material(string name, bool unlit, bool transparent, Color color, Texture2D tex)
        {
            var path = Folder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var mat = new Material(shader) { name = name };
            mat.SetColor("_BaseColor", color);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
            }
            if (transparent)
            {
                MakeTransparent(mat);
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // URP の素材を半透明にする（Inspector の Surface Type: Transparent と同じ設定）
        static void MakeTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            // 半透明の物は影を落とさない
            mat.SetShaderPassEnabled("ShadowCaster", false);
        }
    }
}
