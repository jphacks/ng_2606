using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// モードを選ぶ（Docs/specs/home-modes の REQ-1）。タイトルの次に出る。
    /// 「ステージを つくる」（いままでのホーム）と、「キャラ」「はいけい」（いちらんの画面から移した。2026-11-12）。
    /// （「みんなの ステージ」は 2026-11-07 に、「おだいで つくる」（お題）は 2026-11-12 に消した。
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

            // 左：「ステージを つくる」。右：「キャラ」と「はいけい」を上下に（2026-11-12：「おだいで つくる」のカードの代わり。
            // キャラと背景のボタンは、いちらんの画面から、ここへ移した）
            Card(safe, new Vector2(0.37f, 0.45f), new Vector2(700f, 700f), Theme.Orange, AppText.ModeCreate, AppText.ModeCreateSub, () =>
            {
                Context.Flow.SetMission(null);
                Context.Router.Show(AppScreenId.Home);
            });
            Card(safe, new Vector2(0.71f, 0.45f), new Vector2(600f, 330f), Theme.Blue, AppText.HomeCharacters, AppText.ModeCharactersSub,
                () => Context.Flow.OpenCharacters(), 175f);
            Card(safe, new Vector2(0.71f, 0.45f), new Vector2(600f, 330f), Theme.Green, AppText.HomeBackdrop, AppText.ModeBackdropSub,
                () => Context.Flow.OpenBackdrop(), -195f);
        }

        // カード。at は画面の中の割合の位置、offsetY はそこから上下にずらす量
        void Card(RectTransform parent, Vector2 at, Vector2 size, Color color, string name, string sub, UnityEngine.Events.UnityAction onClick, float offsetY = 0f)
        {
            var card = UiKit.Panel(parent, "Mode", color, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, at, new Vector2(0f, offsetY), size);
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
            // 背の低いカード（キャラ・はいけい）は、名前の場所を広くとる（同じ割合だと、名前が小さくなった）
            var small = size.y < 500f;
            UiKit.Area(label.rectTransform, new Vector2(0f, small ? 0.52f : 0.55f), new Vector2(1f, small ? 0.92f : 0.85f), small ? 16f : 30f);
            var subLabel = UiKit.Label(card.rectTransform, sub, Theme.Body, Theme.WithAlpha(Color.white, 0.95f), TextAlignmentOptions.Center, false, "Sub");
            UiKit.Area(subLabel.rectTransform, new Vector2(0f, small ? 0.08f : 0.12f), new Vector2(1f, 0.52f), small ? 24f : 44f);
        }

        public override void OnBack() => Context.Router.Show(AppScreenId.Title, remember: false);
    }
}
