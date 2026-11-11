using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// いろの しかけ（2026-11-11）。撮る前に、箱の上に貼る折り紙の色ごとに、どんなしかけになるかを見せる。
    /// 撮り方の説明 →「どれくらいの たかさまで のぼれる？」→ この画面 → 撮影、の順に出す。文は、ぜんぶ ひらがな。
    /// 6 つの色を、2 列 × 3 段のカードで並べる。カードには、遊ぶときと同じ模様の見本（GimmickTextures）、色の名前、しかけの名前、ひとことの説明。
    /// しかけの中身は GimmickSystem（ここは説明だけ）。
    /// </summary>
    public class GimmickGuideScreen : AppScreen
    {
        // 2 列 × 3 段（3 列では、カードがせまく、文がはみ出した）
        const int Columns = 2;
        const float CardWidth = 1040f;
        const float CardHeight = 205f;
        const float Gap = 30f;
        const float SwatchSize = 170f;

        struct Entry
        {
            public string Color;
            public string Name;
            public string Text;
            public Color Chip;
            public Texture2D Swatch;
        }

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Cream, Theme.Hex("FFE9C2"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(340f, Theme.SmallButtonHeight),
                () => OnBack(), "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);
            var title = UiKit.Label(safe, AppText.GuideTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1500f, 120f));
            var lead = UiKit.Label(safe, AppText.GuideLead, Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, false, "Lead");
            UiKit.Place(lead.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1800f, 70f));

            var entries = new[]
            {
                new Entry { Color = AppText.GuideBlue, Name = AppText.GuideWaterName, Text = AppText.GuideWater, Chip = Theme.Hex("3B82F6"), Swatch = GimmickTextures.Water(Vector2.right) },
                new Entry { Color = AppText.GuideRed, Name = AppText.GuideFireName, Text = AppText.GuideFire, Chip = Theme.Hex("EF4444"), Swatch = GimmickTextures.Fire() },
                new Entry { Color = AppText.GuideGreen, Name = AppText.GuideHealName, Text = AppText.GuideHeal, Chip = Theme.Hex("22A55E"), Swatch = GimmickTextures.Heal() },
                new Entry { Color = AppText.GuideYellow, Name = AppText.GuideJumpName, Text = AppText.GuideJump, Chip = Theme.Hex("E0A800"), Swatch = GimmickTextures.JumpPad() },
                new Entry { Color = AppText.GuidePurple, Name = AppText.GuideEnemyName, Text = AppText.GuideEnemy, Chip = Theme.Hex("9B5DE5"), Swatch = GimmickTextures.EnemyPad() },
                new Entry { Color = AppText.GuidePink, Name = AppText.GuideWarpName, Text = AppText.GuideWarp, Chip = Theme.Hex("EC4899"), Swatch = GimmickTextures.Warp() },
            };
            var grid = UiKit.NewRect("Grid", safe);
            UiKit.Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(CardWidth * Columns + Gap * (Columns - 1), CardHeight * 3f + Gap * 2f));
            for (var i = 0; i < entries.Length; i++)
            {
                BuildCard(grid, entries[i], i % Columns, i / Columns);
            }

            var go = UiKit.Button(safe, AppText.ReachGo, UiKit.ButtonStyle.Primary, new Vector2(640f, Theme.ButtonHeight),
                () => Context.Flow.BeginCapture(true), "Go", Theme.H1);
            UiKit.Place(go.Rect, new Vector2(1f, 0f), new Vector2(-Theme.Margin, 40f), go.Rect.sizeDelta);
            go.Rect.gameObject.AddComponent<Bobbing>().Amplitude = 6f;
        }

        public override void OnBack() => Context.Router.Back();

        void BuildCard(RectTransform grid, Entry entry, int column, int row)
        {
            var card = UiKit.Panel(grid, "Card" + column + row, Color.white, Theme.CardRadius, shadow: true);
            card.raycastTarget = false;
            UiKit.Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(column * (CardWidth + Gap), -row * (CardHeight + Gap)), new Vector2(CardWidth, CardHeight));
            card.rectTransform.pivot = new Vector2(0f, 1f);
            var area = card.rectTransform;

            // 左：遊ぶときと同じ模様の見本（四角。模様を 2 回ずつ並べる）。ふちに、色の帯
            var frame = UiKit.Panel(area, "SwatchFrame", entry.Chip, 26f);
            frame.raycastTarget = false;
            UiKit.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(SwatchSize, SwatchSize));
            frame.rectTransform.pivot = new Vector2(0f, 0.5f);
            var swatchRect = UiKit.Fill(UiKit.NewRect("Swatch", frame.rectTransform));
            swatchRect.offsetMin = new Vector2(10f, 10f);
            swatchRect.offsetMax = new Vector2(-10f, -10f);
            var swatch = swatchRect.gameObject.AddComponent<RawImage>();
            swatch.texture = entry.Swatch;
            swatch.uvRect = new Rect(0f, 0f, 2f, 2f);
            swatch.raycastTarget = false;
            if (entry.Swatch != null) entry.Swatch.wrapMode = TextureWrapMode.Repeat;

            // 右：色の名前の札、しかけの名前、説明。どれも折り返さず、入りきらないときは字を小さくする
            // （説明の改行は、文の中の \n だけ。言葉の途中で切れないように）
            const float left = 18f + SwatchSize + 30f;
            const float chipWidth = 200f;
            var chip = UiKit.Panel(area, "Chip", entry.Chip, 30f);
            chip.raycastTarget = false;
            UiKit.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(left, -20f), new Vector2(chipWidth, 64f));
            chip.rectTransform.pivot = new Vector2(0f, 1f);
            var chipText = UiKit.FitLabel(chip.rectTransform, entry.Color, Theme.Body, Color.white, TextAlignmentOptions.Center, true, "Color");
            chipText.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Area(chipText.rectTransform, Vector2.zero, Vector2.one, 4f);
            var name = UiKit.FitLabel(area, entry.Name, Theme.H2, Theme.Ink, TextAlignmentOptions.Left, true, "Name");
            name.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(left + chipWidth + 20f, -20f), new Vector2(CardWidth - left - chipWidth - 20f - 24f, 64f));
            name.rectTransform.pivot = new Vector2(0f, 1f);
            var text = UiKit.FitLabel(area, entry.Text, Theme.Body, Theme.Ink, TextAlignmentOptions.TopLeft, false, "Text");
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontSizeMin = Theme.Body * 0.7f;
            UiKit.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(left, -94f), new Vector2(CardWidth - left - 24f, CardHeight - 104f));
            text.rectTransform.pivot = new Vector2(0f, 1f);
        }
    }
}
