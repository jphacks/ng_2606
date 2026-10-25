using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// ならべかた。撮る前に、3 つのことを、絵と短い文で伝える：①うえが たいらな ものを ならべる ②たかい所には ひくい物を となりに ③写真を 2 まい とる。
    /// 写真をインターネットのサービスに送ることも、ここで伝える（子ども向けに、おうちの人と見るよう書く）。
    /// </summary>
    public class HowToScreen : AppScreen
    {
        const float CardWidth = 690f;
        const float CardHeight = 640f;
        const float CardGap = 48f;

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Cream, Theme.Hex("FFE9C2"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(340f, Theme.SmallButtonHeight),
                () => OnBack(), "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);

            var title = UiKit.Label(safe, AppText.HowToTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1400f, 120f));

            var row = UiKit.NewRect("Cards", safe);
            UiKit.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(CardWidth * 3 + CardGap * 2, CardHeight));
            for (var i = 0; i < 3; i++)
            {
                var card = Card(row, i);
                UiKit.Place(card, new Vector2(0f, 0.5f), new Vector2(i * (CardWidth + CardGap), 0f), new Vector2(CardWidth, CardHeight));
            }

            var privacy = UiKit.Label(safe, AppText.HowToPrivacy, Theme.Small, Theme.InkSoft, TextAlignmentOptions.Left, false, "Privacy");
            UiKit.Place(privacy.rectTransform, new Vector2(0f, 0f), new Vector2(Theme.Margin, 40f), new Vector2(1300f, 150f));

            var go = UiKit.Button(safe, AppText.HowToNext, UiKit.ButtonStyle.Primary, new Vector2(720f, Theme.ButtonHeight),
                () => Context.Router.Show(AppScreenId.Reach), "Go", Theme.H1);
            UiKit.Place(go.Rect, new Vector2(1f, 0f), new Vector2(-Theme.Margin, 40f), go.Rect.sizeDelta);
            go.Rect.gameObject.AddComponent<Bobbing>().Amplitude = 6f;
        }

        public override void OnBack() => Context.Router.Show(AppScreenId.Home);

        RectTransform Card(RectTransform parent, int index)
        {
            var panel = UiKit.Panel(parent, "Card" + (index + 1), Color.white, Theme.CardRadius, shadow: true);
            var rect = panel.rectTransform;

            // 絵の場所（空色の板）
            var art = UiKit.Panel(rect, "Art", Theme.SkyLight, 36f);
            UiKit.Area(art.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 1f), 28f);
            switch (index)
            {
                case 0: DrawFlat(art.rectTransform); break;
                case 1: DrawTallLow(art.rectTransform); break;
                default: DrawPhotos(art.rectTransform); break;
            }

            // 番号の札
            var badge = UiKit.Disc(rect, "Number", Theme.Orange, 110f);
            badge.raycastTarget = false;
            UiKit.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -18f), badge.rectTransform.sizeDelta);
            var n = UiKit.Label(badge.rectTransform, (index + 1).ToString(), 66f, Theme.Ink, TextAlignmentOptions.Center, true, "N");
            UiKit.Fill(n.rectTransform);

            var text = index == 0 ? AppText.HowToStep1 : index == 1 ? AppText.HowToStep2 : AppText.HowToStep3;
            var label = UiKit.FitLabel(rect, text, Theme.Body, Theme.Ink, TextAlignmentOptions.Center, true, "Text");
            UiKit.Area(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.36f), 24f);
            return rect;
        }

        // ---- 絵（四角と丸だけで描く）----

        static Image Piece(RectTransform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size, float radius, float angle = 0f)
        {
            var image = UiKit.Panel(parent, name, color, radius);
            UiKit.Place(image.rectTransform, anchor, pos, size);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return image;
        }

        static void Desk(RectTransform art)
        {
            Piece(art, "Desk", Theme.Wood, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(560f, 70f), 24f);
        }

        // ① うえが たいらな ものを ならべる
        static void DrawFlat(RectTransform art)
        {
            Desk(art);
            Piece(art, "A", Theme.BlockColors[0], new Vector2(0.5f, 0f), new Vector2(-170f, 94f), new Vector2(150f, 150f), 26f);
            Piece(art, "B", Theme.BlockColors[3], new Vector2(0.5f, 0f), new Vector2(10f, 94f), new Vector2(170f, 110f), 24f);
            Piece(art, "C", Theme.BlockColors[2], new Vector2(0.5f, 0f), new Vector2(190f, 94f), new Vector2(130f, 130f), 24f);
            // ならんだ しるし（うえから みた めやすの 線）
            Piece(art, "Hint", Theme.WithAlpha(Theme.Orange, 0.9f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(420f, 14f), 7f);
        }

        // ② たかい ところには ひくい ものを となりに
        static void DrawTallLow(RectTransform art)
        {
            Desk(art);
            Piece(art, "Tall", Theme.BlockColors[0], new Vector2(0.5f, 0f), new Vector2(-120f, 94f), new Vector2(150f, 260f), 26f);
            Piece(art, "Low", Theme.BlockColors[1], new Vector2(0.5f, 0f), new Vector2(60f, 94f), new Vector2(170f, 90f), 24f);
            Piece(art, "Lower", Theme.BlockColors[2], new Vector2(0.5f, 0f), new Vector2(230f, 94f), new Vector2(120f, 50f), 20f);
            // ちいさな キャラ（のぼる）
            var body = Piece(art, "Hero", Theme.Blue, new Vector2(0.5f, 0f), new Vector2(230f, 144f), new Vector2(60f, 80f), 28f);
            var eye1 = UiKit.Disc(body.rectTransform, "E1", Color.white, 18f);
            UiKit.Place(eye1.rectTransform, new Vector2(0.3f, 0.65f), Vector2.zero, eye1.rectTransform.sizeDelta);
            var eye2 = UiKit.Disc(body.rectTransform, "E2", Color.white, 18f);
            UiKit.Place(eye2.rectTransform, new Vector2(0.7f, 0.65f), Vector2.zero, eye2.rectTransform.sizeDelta);
        }

        // ③ しゃしんを 2まい とる
        static void DrawPhotos(RectTransform art)
        {
            // スマホ
            var phone = Piece(art, "Phone", Theme.Navy, new Vector2(0.5f, 0.5f), new Vector2(-150f, 0f), new Vector2(150f, 270f), 30f, 8f);
            var screen = UiKit.Panel(phone.rectTransform, "Screen", Theme.Sky, 18f);
            UiKit.Area(screen.rectTransform, Vector2.zero, Vector2.one, 12f);
            var lens = UiKit.Disc(phone.rectTransform, "Lens", Theme.Orange, 30f);
            UiKit.Place(lens.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), lens.rectTransform.sizeDelta);
            // しゃしん 2 まい
            for (var i = 0; i < 2; i++)
            {
                var photo = Piece(art, "Photo" + (i + 1), Color.white, new Vector2(0.5f, 0.5f),
                    new Vector2(90f + i * 130f, 40f - i * 70f), new Vector2(200f, 150f), 18f, i == 0 ? 6f : -5f);
                var inner = UiKit.Panel(photo.rectTransform, "In", i == 0 ? Theme.BlockColors[3] : Theme.BlockColors[1], 12f);
                UiKit.Area(inner.rectTransform, Vector2.zero, Vector2.one, 10f);
                var n = UiKit.Label(photo.rectTransform, (i + 1).ToString(), 64f, Color.white, TextAlignmentOptions.Center, true, "N");
                UiKit.Fill(n.rectTransform);
            }
        }
    }
}
