using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// タイトル。アイキャッチ（Downloads/eyecatch.png）に合わせた景色：灰ベージュの壁と、木目の机。
    /// 机の上に、赤い本、木の積み木、段ボールの箱が並び、積み木の上にキャラが立つ。左上に「ならべて！ステージ」。
    /// 「はじめる」でホームへ。
    /// </summary>
    public class TitleScreen : AppScreen
    {
        // 景色の色
        static readonly Color WallTop = Theme.Hex("9D9590");
        static readonly Color WallBottom = Theme.Hex("CBBDAE");
        static readonly Color DeskBase = Theme.Hex("C98A4B");
        static readonly Color DeskEdge = Theme.Hex("A86E36");
        static readonly Color TitleBrown = Theme.Hex("4A3226");
        static readonly Color TitleRed = Theme.Hex("E8453C");

        // 机のふち（奥）の高さ（画面の割合）
        const float DeskTop = 0.50f;

        protected override void Build()
        {
            if (Context.TitleImage != null)
            {
                BuildPhoto();
                return;
            }
            BuildDrawn();
        }

        // 写真（アイキャッチ）をそのまま出す。題字も写真に入っているので、重ねるのは「はじめる」だけ。
        // 画面の縦横の比が違うときは、写真の上下（または左右）を、同じだけ切って、画面いっぱいにする
        void BuildPhoto()
        {
            var back = UiKit.Fill(UiKit.NewRect("Photo", Rect));
            var raw = back.gameObject.AddComponent<RawImage>();
            raw.texture = Context.TitleImage;
            raw.raycastTarget = true;
            back.gameObject.AddComponent<CoverUv>().Apply();

            var start = UiKit.Button(Rect, AppText.Start, UiKit.ButtonStyle.Primary, new Vector2(540f, 170f),
                () => Context.Router.Show(AppScreenId.Home), "Start", Theme.H1);
            UiKit.Place(start.Rect, new Vector2(0.445f, 0f), new Vector2(0f, 70f), start.Rect.sizeDelta);
            start.Rect.gameObject.AddComponent<Bobbing>().Amplitude = 8f;
        }

        // 写真が無いときの、コードで描いた景色
        void BuildDrawn()
        {
            // 壁
            UiKit.Background(Rect, "Wall", WallTop, WallBottom);

            // 机（木目つき）
            var desk = UiKit.Panel(Rect, "Desk", DeskBase, 0f);
            UiKit.Area(desk.rectTransform, new Vector2(-0.05f, -0.1f), new Vector2(1.05f, DeskTop));
            desk.sprite = UiKit.GradientSprite(Theme.Hex("D49A58"), Theme.Hex("B87A3E"));
            desk.type = Image.Type.Simple;
            desk.color = Color.white; // 色は、グラデーションの絵が持つ（板の色が掛かって暗くならないよう）
            desk.raycastTarget = false;
            var edge = UiKit.NewRect("DeskEdge", Rect);
            UiKit.Area(edge, new Vector2(-0.05f, DeskTop - 0.012f), new Vector2(1.05f, DeskTop + 0.004f));
            var edgeImage = edge.gameObject.AddComponent<Image>();
            edgeImage.color = DeskEdge;
            edgeImage.raycastTarget = false;
            Grain();

            // 机の上の物（奥から手前の順）
            BuildBox(0.64f, 0.285f);
            BuildCube(0.47f, 0.20f);
            BuildBook(0.20f, 0.09f);

            // 題字
            var shadow = UiKit.Label(Rect, AppText.TitlePlain, 150f, new Color(0f, 0f, 0f, 0.16f), TextAlignmentOptions.Left, true, "TitleShadow");
            UiKit.Place(shadow.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -94f), new Vector2(1800f, 260f));
            var title = UiKit.Label(Rect, AppText.TitleRich, 150f, TitleBrown, TextAlignmentOptions.Left, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(144f, -88f), new Vector2(1800f, 260f));
            var tagline = UiKit.Label(Rect, AppText.TitleTagline, 56f, TitleBrown, TextAlignmentOptions.Left, true, "Tagline");
            UiKit.Place(tagline.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -350f), new Vector2(1800f, 100f));

            // はじめる
            var start = UiKit.Button(Rect, AppText.Start, UiKit.ButtonStyle.Primary, new Vector2(660f, 190f),
                () => Context.Router.Show(AppScreenId.Home), "Start", Theme.H1);
            UiKit.Place(start.Rect, new Vector2(1f, 0f), new Vector2(-130f, 90f), start.Rect.sizeDelta);
            start.Rect.gameObject.AddComponent<Bobbing>().Amplitude = 8f;
        }

        // 机の木目：細い横線を、長さと濃さを変えて、たくさん引く（毎回同じ並び）
        void Grain()
        {
            var holder = UiKit.NewRect("Grain", Rect);
            UiKit.Area(holder, new Vector2(0f, 0f), new Vector2(1f, DeskTop - 0.014f));
            var seed = 7;
            for (var i = 0; i < 70; i++)
            {
                seed = seed * 1103515245 + 12345;
                var r1 = ((seed >> 8) & 0xFFFF) / 65535f;
                seed = seed * 1103515245 + 12345;
                var r2 = ((seed >> 8) & 0xFFFF) / 65535f;
                seed = seed * 1103515245 + 12345;
                var r3 = ((seed >> 8) & 0xFFFF) / 65535f;
                var line = UiKit.NewRect("G" + i, holder);
                var dark = i % 3 != 0;
                var image = line.gameObject.AddComponent<Image>();
                image.color = dark ? new Color(0.45f, 0.25f, 0.08f, 0.12f + 0.12f * r3) : new Color(1f, 0.85f, 0.6f, 0.10f);
                image.raycastTarget = false;
                var width = 0.25f + 0.6f * r2;
                var x = r1 * (1f - width);
                line.anchorMin = line.anchorMax = new Vector2(x + width * 0.5f, r3);
                line.sizeDelta = new Vector2(width * 2532f, 2f + 5f * r2);
                line.localRotation = Quaternion.Euler(0f, 0f, (r1 - 0.5f) * 1.2f);
            }
        }

        // 足もとの影
        void Shadow(RectTransform parent, Vector2 baseAnchor, float width)
        {
            var s = UiKit.Disc(parent, "Shadow", new Color(0.25f, 0.12f, 0f, 0.28f), 100f);
            UiKit.Place(s.rectTransform, baseAnchor, new Vector2(0f, -6f), new Vector2(width * 1.1f, width * 0.16f));
        }

        // 段ボールの箱
        void BuildBox(float x, float baseY)
        {
            var root = UiKit.NewRect("Box", Rect);
            UiKit.Place(root, new Vector2(x, baseY), Vector2.zero, new Vector2(560f, 430f));
            root.pivot = new Vector2(0.5f, 0f);
            Shadow(root, new Vector2(0.5f, 0f), 560f);
            var body = UiKit.Panel(root, "Body", Theme.Hex("C99A63"), 10f);
            UiKit.Fill(body.rectTransform);
            body.sprite = UiKit.GradientSprite(Theme.Hex("D2A46C"), Theme.Hex("BC8C57"));
            body.type = Image.Type.Simple;
            body.color = Color.white;
            // ふたの合わせ目と、ガムテープ
            var seam = UiKit.NewRect("Seam", root);
            UiKit.Place(seam, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(560f, 6f));
            seam.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.25f, 0.1f, 0.35f);
            var tape = UiKit.Panel(root, "Tape", Theme.Hex("D7B27A"), 6f);
            UiKit.Place(tape.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(120f, 190f));
            var tapeShine = UiKit.NewRect("Shine", tape.rectTransform);
            UiKit.Place(tapeShine, new Vector2(0.3f, 0.5f), Vector2.zero, new Vector2(18f, 170f));
            tapeShine.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
        }

        // 木の積み木（上にキャラが立つ）
        void BuildCube(float x, float baseY)
        {
            var root = UiKit.NewRect("Cube", Rect);
            UiKit.Place(root, new Vector2(x, baseY), Vector2.zero, new Vector2(250f, 250f));
            root.pivot = new Vector2(0.5f, 0f);
            Shadow(root, new Vector2(0.5f, 0f), 250f);
            var body = UiKit.Panel(root, "Body", Theme.Hex("E8C58E"), 22f, shadow: true);
            UiKit.Fill(body.rectTransform);
            for (var i = 0; i < 6; i++)
            {
                var ring = UiKit.Panel(root, "Grain" + i, new Color(0.75f, 0.5f, 0.2f, 0.25f), 20f);
                UiKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, (i - 2.5f) * 32f), new Vector2(190f - i * 6f, 6f));
                ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1 : -1) * (3f + i));
                ring.raycastTarget = false;
            }
            // キャラ（積み木の上）
            var hero = UiKit.Panel(root, "Hero", Theme.Blue, 56f, shadow: true);
            UiKit.Place(hero.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(110f, 150f));
            hero.rectTransform.pivot = new Vector2(0.5f, 0f);
            hero.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            hero.raycastTarget = false;
            hero.gameObject.AddComponent<Bobbing>().Amplitude = 10f;
            foreach (var dx in new[] { -22f, 22f })
            {
                var eye = UiKit.Disc(hero.rectTransform, "Eye", Color.white, 36f);
                UiKit.Place(eye.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(dx, 0f), eye.rectTransform.sizeDelta);
                var pupil = UiKit.Disc(eye.rectTransform, "Pupil", Theme.Ink, 17f);
                UiKit.Place(pupil.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, pupil.rectTransform.sizeDelta);
            }
        }

        // 赤い本（横たわっている）
        void BuildBook(float x, float baseY)
        {
            var root = UiKit.NewRect("Book", Rect);
            UiKit.Place(root, new Vector2(x, baseY), Vector2.zero, new Vector2(640f, 190f));
            root.pivot = new Vector2(0.5f, 0f);
            root.localRotation = Quaternion.Euler(0f, 0f, -2f);
            Shadow(root, new Vector2(0.5f, 0f), 640f);
            var pages = UiKit.Panel(root, "Pages", Theme.Hex("F4EBD8"), 12f);
            UiKit.Place(pages.rectTransform, new Vector2(0.5f, 0f), new Vector2(10f, 0f), new Vector2(600f, 80f));
            for (var i = 1; i < 6; i++)
            {
                var line = UiKit.NewRect("Page" + i, pages.rectTransform);
                UiKit.Place(line, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f + i * 10f), new Vector2(560f, 2f));
                line.gameObject.AddComponent<Image>().color = new Color(0.6f, 0.5f, 0.35f, 0.25f);
            }
            var cover = UiKit.Panel(root, "Cover", Theme.Hex("C94A42"), 14f, shadow: true);
            UiKit.Place(cover.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(640f, 120f));
            var spine = UiKit.Panel(cover.rectTransform, "Spine", Theme.Hex("A83A34"), 10f);
            UiKit.Place(spine.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(46f, 120f));
        }
    }
}
