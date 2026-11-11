using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// モードを選ぶ（Docs/specs/home-modes の REQ-1）。タイトルの次に出る。
    /// 「ステージを つくる」（いままでのホーム）。（「みんなの ステージ」は 2026-11-07 に、「おだいで つくる」（お題）は 2026-11-12 に消した。
    /// お題の画面 MissionScreen と、お題の決まりは残してあるので、カードを戻せば、また使える）
    /// </summary>
    public class ModeScreen : AppScreen
    {
        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Cream, Theme.Hex("FFE9C2"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(330f, Theme.SmallButtonHeight),
                OnBack, "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);
            var title = UiKit.Label(safe, AppText.ModeTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1400f, 120f));

            // 「おだいで つくる」のカードは出さない（2026-11-12）。「ステージを つくる」だけを、真ん中に
            Card(safe, 0.5f, Theme.Orange, AppText.ModeCreate, AppText.ModeCreateSub, () =>
            {
                Context.Flow.SetMission(null);
                Context.Router.Show(AppScreenId.Home);
            });
        }

        void Card(RectTransform parent, float x, Color color, string name, string sub, UnityEngine.Events.UnityAction onClick)
        {
            var card = UiKit.Panel(parent, "Mode", color, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(x, 0.45f), Vector2.zero, new Vector2(700f, 700f));
            card.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                UiSound.Play(UiSound.Cue.Click);
                onClick();
            });
            card.gameObject.AddComponent<PressScale>();
            var label = UiKit.FitLabel(card.rectTransform, name, Theme.H1, Color.white, TextAlignmentOptions.Center, true, "Name");
            UiKit.Area(label.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 0.85f), 30f);
            var subLabel = UiKit.Label(card.rectTransform, sub, Theme.Body, Theme.WithAlpha(Color.white, 0.95f), TextAlignmentOptions.Center, false, "Sub");
            UiKit.Area(subLabel.rectTransform, new Vector2(0f, 0.12f), new Vector2(1f, 0.52f), 44f);
        }

        public override void OnBack() => Context.Router.Show(AppScreenId.Title, remember: false);
    }
}
