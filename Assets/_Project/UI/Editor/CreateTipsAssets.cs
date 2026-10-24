using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeskStage.UI.Editor
{
    /// <summary>
    /// メニュー「DeskStage/D/コツの絵を作る」（Step 9）。
    /// 仮の絵（図形だけの簡単な絵）と、絵の一覧 TipsPages を作る。絵は人が後で同じ名前の画像に差し替える。
    /// もうある絵と一覧は作り直さない（差し替えた絵や、Enabled の切り替えを消さない）。
    /// </summary>
    public static class CreateTipsAssets
    {
        const string ImageFolder = "Assets/_Project/UI/Images";
        public const string TipsPagesPath = "Assets/_Project/UI/TipsPages.asset";
        const int W = 640;
        const int H = 400;

        static readonly Color Paper = new Color(1f, 0.98f, 0.93f);
        static readonly Color DeskColor = new Color(0.78f, 0.62f, 0.44f);
        static readonly Color Cardboard = new Color(0.82f, 0.66f, 0.45f);
        static readonly Color Gray = new Color(0.7f, 0.7f, 0.7f);
        static readonly Color Green = new Color(0.4f, 0.78f, 0.4f);
        static readonly Color Red = new Color(0.92f, 0.36f, 0.32f);
        static readonly Color Character = new Color(1f, 0.6f, 0.2f);

        [MenuItem("DeskStage/D/コツの絵を作る")]
        public static void Run()
        {
            Directory.CreateDirectory(ImageFolder);
            var flat = Picture("Tips_Flat", DrawFlat);
            var height = Picture("Tips_Height", DrawHeight);
            var stairs = Picture("Tips_Stairs", DrawStairs);
            var planB = Picture("Tips_PlanB", DrawPlanB);
            var planC = Picture("Tips_PlanC", DrawPlanC);
            var hard = Picture("Tips_Hard", DrawHard);

            var asset = AssetDatabase.LoadAssetAtPath<TipsPages>(TipsPagesPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TipsPages>();
                asset.Pages.Add(new TipsPages.Page { Key = "tips.flat", Image = flat });
                asset.Pages.Add(new TipsPages.Page { Key = "tips.height", Image = height });
                asset.Pages.Add(new TipsPages.Page { Key = "tips.stairs", Image = stairs });
                // P0 の判定で案 B・C になったら Enabled をオンにする。見つけにくい物の絵も、必要なら出す
                asset.Pages.Add(new TipsPages.Page { Key = "tips.planB", Image = planB, Enabled = false });
                asset.Pages.Add(new TipsPages.Page { Key = "tips.planC", Image = planC, Enabled = false });
                asset.Pages.Add(new TipsPages.Page { Key = "tips.hard", Image = hard, Enabled = false });
                AssetDatabase.CreateAsset(asset, TipsPagesPath);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("コツの絵を作りました: " + ImageFolder);
        }

        static Sprite Picture(string name, System.Action<Texture2D> draw)
        {
            var path = ImageFolder + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                Fill(tex, 0, 0, W, H, Paper);
                draw(tex);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 下が y=0。机の板は下の帯
        static void Desk(Texture2D t) => Fill(t, 20, 30, W - 40, 30, DeskColor);

        static void DrawFlat(Texture2D t)
        {
            Desk(t);
            Box(t, 70, 60, 170, 110, Cardboard);                        // 段ボール箱
            Box(t, 280, 60, 60, 60, new Color(0.35f, 0.6f, 0.95f));     // 積み木
            Box(t, 340, 60, 60, 60, new Color(1f, 0.8f, 0.3f));
            Box(t, 310, 120, 60, 60, Green);
            Box(t, 450, 60, 130, 30, new Color(0.55f, 0.4f, 0.75f));    // 本
            Box(t, 455, 90, 120, 25, new Color(0.95f, 0.55f, 0.55f));
        }

        static void DrawHeight(Texture2D t)
        {
            // たかさものさし：1cm を 36px。0〜0.8 灰、0.8〜4.5 緑、4.5〜8 赤
            const float px = 36f;
            int x = 230;
            Fill(t, x, 40, 90, (int)(0.8f * px), Gray);
            Fill(t, x, 40 + (int)(0.8f * px), 90, (int)(3.7f * px), Green);
            Fill(t, x, 40 + (int)(4.5f * px), 90, (int)(3.5f * px), Red);
            // 実物の比のキャラ（身長3cm）
            Figure(t, 380, 40, (int)(3f * px));
        }

        static void DrawStairs(Texture2D t)
        {
            Desk(t);
            Box(t, 380, 60, 160, 200, Cardboard);                       // 高い箱
            Box(t, 250, 60, 130, 110, new Color(0.35f, 0.6f, 0.95f));   // 低い物を隣に
            Box(t, 150, 60, 100, 50, Green);
            Figure(t, 90, 60, 60);
        }

        static void DrawPlanB(Texture2D t)
        {
            Desk(t);
            Box(t, 110, 60, 220, 120, Cardboard);
            // てのひら（丸と指の帯）
            Fill(t, 400, 90, 110, 110, new Color(1f, 0.85f, 0.7f));
            for (int i = 0; i < 4; i++)
            {
                Fill(t, 400 + i * 28, 200, 20, 60, new Color(1f, 0.85f, 0.7f));
            }
        }

        static void DrawPlanC(Texture2D t)
        {
            Desk(t);
            Box(t, 170, 60, 300, 130, Cardboard);
            // 上にのせた模様のシート
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Fill(t, 170 + x * 30, 190 + y * 10, 30, 10, (x + y) % 2 == 0 ? Color.black : Color.white);
                }
            }
        }

        static void DrawHard(Texture2D t)
        {
            Desk(t);
            Box(t, 80, 60, 140, 110, new Color(0.1f, 0.1f, 0.1f));      // 黒い
            Box(t, 260, 60, 140, 110, new Color(0.75f, 0.8f, 0.85f));   // ぴかぴか
            Fill(t, 280, 120, 30, 40, Color.white);
            Outline(t, 440, 60, 140, 110, new Color(0.5f, 0.75f, 0.9f)); // すきとおった
        }

        static void Figure(Texture2D t, int x, int y, int height)
        {
            var body = height * 2 / 3;
            Fill(t, x, y, height / 3, body, Character);
            Fill(t, x - height / 12, y + body, height / 2, height / 3, Character);
        }

        static void Box(Texture2D t, int x, int y, int w, int h, Color c)
        {
            Fill(t, x, y, w, h, c);
            Outline(t, x, y, w, h, c * 0.6f);
        }

        static void Outline(Texture2D t, int x, int y, int w, int h, Color c)
        {
            Fill(t, x, y, w, 4, c);
            Fill(t, x, y + h - 4, w, 4, c);
            Fill(t, x, y, 4, h, c);
            Fill(t, x + w - 4, y, 4, h, c);
        }

        static void Fill(Texture2D t, int x, int y, int w, int h, Color c)
        {
            c.a = 1f;
            for (int j = Mathf.Max(0, y); j < Mathf.Min(H, y + h); j++)
            {
                for (int i = Mathf.Max(0, x); i < Mathf.Min(W, x + w); i++)
                {
                    t.SetPixel(i, j, c);
                }
            }
        }
    }
}
