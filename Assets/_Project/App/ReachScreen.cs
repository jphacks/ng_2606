using DeskStage.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// 撮る前に、「どれくらいの高さの ものなら キャラが のぼれるか」を見せる。
    /// 左：ものさし（0〜30cm）に、あるいて のぼれる／ジャンプで のぼれる／のぼれない、の 3 つの色。キャラ（身長 3cm）も、同じ縮尺で立てる。
    /// 右：それぞれの意味と、身のまわりの物の高さのめやす。
    /// 数字は、C の StepRules（歩ける段の上限、跳べる段の上限）から取る。2 段ジャンプなどで変わっても、ここは自動で合う。
    /// </summary>
    public class ReachScreen : AppScreen
    {
        const float ScaleMaxCm = 30f;
        const float GaugeHeight = 760f;
        const float GaugeWidth = 150f;
        const float CharacterHeightCm = 3f;
        const float CharacterWidthCm = 1.6f;

        static readonly Color WalkColor = Theme.Hex("7ED6A5");
        static readonly Color JumpColor = Theme.Hex("FFC24D");
        static readonly Color WallColor = Theme.Hex("EF4B6C");

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Cream, Theme.Hex("FFE9C2"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(340f, Theme.SmallButtonHeight),
                () => OnBack(), "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);
            var title = UiKit.Label(safe, AppText.ReachTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1500f, 120f));

            var walkCm = StepRules.DefaultWalkMax * 100f;
            var jumpCm = StepRules.DefaultJumpMax * 100f;

            BuildGauge(safe, walkCm, jumpCm);

            // 右：3 つの意味
            var rows = UiKit.NewRect("Rows", safe);
            UiKit.Place(rows, new Vector2(0.64f, 0.5f), new Vector2(0f, 20f), new Vector2(1280f, 700f));
            Row(rows, 0, WalkColor, AppText.ReachWalkBadge, string.Format(AppText.ReachWalk, FormatCm(walkCm)), 0f, 150f, 42f);
            Row(rows, 1, JumpColor, AppText.ReachJumpBadge, string.Format(AppText.ReachJump, FormatCm(jumpCm)), 170f, 150f, 42f);
            Row(rows, 2, WallColor, AppText.ReachWallBadge, string.Format(AppText.ReachWall, FormatCm(jumpCm)), 340f, 150f, 42f);
            Row(rows, 3, Theme.Hex("B58CFF"), AppText.ReachGimmickBadge, AppText.GimmickHint, 510f, 190f, 32f);

            var examples = UiKit.Label(safe, AppText.ReachExamples, Theme.Small, Theme.InkSoft, TextAlignmentOptions.Left, false, "Examples");
            UiKit.Place(examples.rectTransform, new Vector2(0f, 0f), new Vector2(Theme.Margin, 50f), new Vector2(1500f, 70f));

            var go = UiKit.Button(safe, AppText.ReachGo, UiKit.ButtonStyle.Primary, new Vector2(640f, Theme.ButtonHeight),
                () => Context.Flow.BeginCapture(true), "Go", Theme.H1);
            UiKit.Place(go.Rect, new Vector2(1f, 0f), new Vector2(-Theme.Margin, 40f), go.Rect.sizeDelta);
            go.Rect.gameObject.AddComponent<Bobbing>().Amplitude = 6f;
        }

        public override void OnBack() => Context.Router.Back();

        // 0.8 は「0.8」、22 は「22」と出す
        static string FormatCm(float cm) => Mathf.Approximately(cm, Mathf.Round(cm)) ? Mathf.RoundToInt(cm).ToString() : cm.ToString("0.#");

        // ---- ものさし ----

        void BuildGauge(RectTransform safe, float walkCm, float jumpCm)
        {
            var gauge = UiKit.NewRect("Gauge", safe);
            UiKit.Place(gauge, new Vector2(0.17f, 0.5f), new Vector2(0f, -30f), new Vector2(GaugeWidth, GaugeHeight));

            var frame = UiKit.Panel(gauge, "Frame", Color.white, 30f, shadow: true);
            UiKit.Fill(frame.rectTransform);
            frame.rectTransform.offsetMin = new Vector2(-14f, -14f);
            frame.rectTransform.offsetMax = new Vector2(14f, 14f);

            Zone(gauge, 0f, walkCm, WalkColor, "Walk");
            Zone(gauge, walkCm, jumpCm, JumpColor, "Jump");
            Zone(gauge, jumpCm, ScaleMaxCm, WallColor, "Wall");

            // 目盛り（5cm ごと）
            for (var cm = 0; cm <= (int)ScaleMaxCm; cm += 5)
            {
                var y = cm / ScaleMaxCm;
                var tick = UiKit.NewRect("Tick" + cm, gauge);
                tick.anchorMin = tick.anchorMax = new Vector2(0f, y);
                tick.pivot = new Vector2(1f, 0.5f);
                tick.anchoredPosition = new Vector2(-18f, 0f);
                tick.sizeDelta = new Vector2(34f, 4f);
                var tickImage = tick.gameObject.AddComponent<Image>();
                tickImage.color = Theme.Ink;
                tickImage.raycastTarget = false;
                var label = UiKit.Label(gauge, cm + "cm", Theme.Small, Theme.Ink, TextAlignmentOptions.Right, true, "L" + cm);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, y);
                label.rectTransform.pivot = new Vector2(1f, 0.5f);
                label.rectTransform.anchoredPosition = new Vector2(-60f, 0f);
                label.rectTransform.sizeDelta = new Vector2(150f, 50f);
            }

            // キャラ（身長 3cm）を、同じ縮尺で、ものさしの右に立てる
            var scale = GaugeHeight / ScaleMaxCm;
            var hero = UiKit.Panel(gauge, "Hero", Theme.Blue, CharacterWidthCm * scale * 0.5f, shadow: true);
            hero.rectTransform.anchorMin = hero.rectTransform.anchorMax = new Vector2(1f, 0f);
            hero.rectTransform.pivot = new Vector2(0f, 0f);
            hero.rectTransform.anchoredPosition = new Vector2(30f, 0f);
            hero.rectTransform.sizeDelta = new Vector2(CharacterWidthCm * scale, CharacterHeightCm * scale);
            hero.raycastTarget = false;
            foreach (var dx in new[] { 0.3f, 0.7f })
            {
                var eye = UiKit.Disc(hero.rectTransform, "Eye", Color.white, 18f);
                UiKit.Place(eye.rectTransform, new Vector2(dx, 0.68f), Vector2.zero, eye.rectTransform.sizeDelta);
                var pupil = UiKit.Disc(eye.rectTransform, "Pupil", Theme.Ink, 9f);
                UiKit.Place(pupil.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, pupil.rectTransform.sizeDelta);
            }
            var heroLabel = UiKit.Label(gauge, AppText.ReachHero, Theme.Small, Theme.InkSoft, TextAlignmentOptions.Left, true, "HeroLabel");
            heroLabel.rectTransform.anchorMin = heroLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            heroLabel.rectTransform.pivot = new Vector2(0f, 0f);
            heroLabel.rectTransform.anchoredPosition = new Vector2(90f, 8f);
            heroLabel.rectTransform.sizeDelta = new Vector2(300f, 80f);
        }

        static void Zone(RectTransform gauge, float fromCm, float toCm, Color color, string name)
        {
            var zone = UiKit.NewRect(name, gauge);
            zone.anchorMin = new Vector2(0f, fromCm / ScaleMaxCm);
            zone.anchorMax = new Vector2(1f, toCm / ScaleMaxCm);
            zone.offsetMin = Vector2.zero;
            zone.offsetMax = Vector2.zero;
            var image = zone.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        // ---- 右の説明 ----

        void Row(RectTransform parent, int index, Color color, string badgeText, string text, float top, float height, float fontSize)
        {
            var card = UiKit.Panel(parent, "Row" + index, Color.white, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(1280f, height));
            card.rectTransform.pivot = new Vector2(0.5f, 1f);

            var badge = UiKit.Panel(card.rectTransform, "Badge", color, 60f);
            UiKit.Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(330f, 110f));
            var badgeLabel = UiKit.FitLabel(badge.rectTransform, badgeText, Theme.Body, index >= 2 ? Color.white : Theme.Ink, TextAlignmentOptions.Center, true, "L");
            UiKit.Fill(badgeLabel.rectTransform);
            badgeLabel.rectTransform.offsetMin = new Vector2(14f, 6f);
            badgeLabel.rectTransform.offsetMax = new Vector2(-14f, -6f);

            var label = UiKit.Label(card.rectTransform, text, fontSize, Theme.Ink, TextAlignmentOptions.Left, true, "Text");
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Fill(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(400f, 10f);
            label.rectTransform.offsetMax = new Vector2(-24f, -10f);
        }
    }
}
