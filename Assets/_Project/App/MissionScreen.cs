using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// おだいで つくる（Docs/specs/home-modes の REQ-3・5）。お題を並べ、達成した物にスタンプを出す。
    /// 選ぶと、そのお題を決めて、いつもの撮影の流れ（撮り方の説明から）へ進む。
    /// </summary>
    public class MissionScreen : AppScreen
    {
        const float CardWidth = 520f;

        RectTransform content;
        readonly List<GameObject> cards = new List<GameObject>();

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.SkyLight, Theme.Sky);
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));
            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(330f, Theme.SmallButtonHeight),
                OnBack, "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);
            var title = UiKit.Label(safe, AppText.MissionTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1400f, 120f));

            var area = UiKit.NewRect("Scroll", safe);
            UiKit.Area(area, new Vector2(0f, 0.05f), new Vector2(1f, 0.82f));
            content = CardList.Create(area, 40f);
        }

        public override void OnEnter(object arg)
        {
            foreach (var c in cards) UiKit.Kill(c);
            cards.Clear();
            for (var i = 0; i < MissionCatalog.All.Length; i++)
            {
                cards.Add(BuildCard(MissionCatalog.All[i], i));
            }
        }

        public override void OnBack()
        {
            Context.Flow.SetMission(null);
            Context.Router.Show(AppScreenId.Mode, remember: false);
        }

        GameObject BuildCard(Mission mission, int index)
        {
            var done = MissionStamps.Has(mission.Id);
            var card = UiKit.Panel(content, "Mission", Theme.Card, Theme.CardRadius, shadow: true);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, 0f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                UiSound.Play(UiSound.Cue.Click);
                Context.Flow.SetMission(mission);
                Context.Flow.StartNewStage();
            });
            card.gameObject.AddComponent<PressScale>();

            var number = UiKit.Disc(card.rectTransform, "Number", Theme.BlockColors[index % Theme.BlockColors.Length], 120f);
            UiKit.Place(number.rectTransform, new Vector2(0.5f, 0.8f), Vector2.zero, number.rectTransform.sizeDelta);
            number.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            number.raycastTarget = false;
            var numberLabel = UiKit.Label(number.rectTransform, (index + 1).ToString(), Theme.H1, Color.white, TextAlignmentOptions.Center, true, "N");
            UiKit.Fill(numberLabel.rectTransform);

            var name = UiKit.FitLabel(card.rectTransform, mission.Name, Theme.H2, Theme.Ink, TextAlignmentOptions.Center, true, "Name");
            UiKit.Area(name.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 0.68f), 24f);

            var lines = new List<string>();
            foreach (var c in mission.Conditions) lines.Add("・" + c.Text);
            var conditions = UiKit.Label(card.rectTransform, string.Join("\n", lines), Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, false, "Conditions");
            UiKit.Area(conditions.rectTransform, new Vector2(0f, 0.2f), new Vector2(1f, 0.54f), 30f);

            if (done)
            {
                // スタンプ（少し傾けた、赤い丸のはんこ）
                var stamp = UiKit.Disc(card.rectTransform, "Stamp", Theme.WithAlpha(Theme.Red, 0.9f), 200f);
                UiKit.Place(stamp.rectTransform, new Vector2(0.72f, 0.12f), Vector2.zero, stamp.rectTransform.sizeDelta);
                stamp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 14f);
                stamp.raycastTarget = false;
                var stampLabel = UiKit.FitLabel(stamp.rectTransform, AppText.MissionStamp, Theme.H2, Color.white, TextAlignmentOptions.Center, true, "S");
                UiKit.Fill(stampLabel.rectTransform);
            }
            return card.gameObject;
        }
    }
}
