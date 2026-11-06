using System.Collections.Generic;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// みんなの ステージ（Docs/specs/home-modes の REQ-10・11）。この Mac のサーバーから、投稿されたステージの一覧を読み、選んで遊ぶ。
    /// 一覧の図は、1 件ずつ読んでから描く（写真は無い）。記録は、この iPhone の中の自己ベスト。
    /// </summary>
    public class CommunityScreen : AppScreen
    {
        const float CardWidth = 520f;
        const int MaxCards = 30;

        RectTransform content;
        TMP_Text status;
        readonly List<GameObject> cards = new List<GameObject>();
        readonly Dictionary<string, StageData> loaded = new Dictionary<string, StageData>();
        int generation;

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Hex("E7F8EF"), Theme.Hex("C6EEDA"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));
            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(330f, Theme.SmallButtonHeight),
                OnBack, "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);
            var title = UiKit.Label(safe, AppText.CommunityTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1400f, 120f));
            var reload = UiKit.Button(safe, AppText.CommunityReload, UiKit.ButtonStyle.Soft, new Vector2(380f, Theme.SmallButtonHeight),
                Load, "Reload", Theme.Body);
            UiKit.Place(reload.Rect, new Vector2(1f, 1f), new Vector2(-Theme.Margin, -45f), reload.Rect.sizeDelta);

            var area = UiKit.NewRect("Scroll", safe);
            UiKit.Area(area, new Vector2(0f, 0.03f), new Vector2(1f, 0.83f));
            content = CardList.Create(area, 40f);

            status = UiKit.Label(safe, "", Theme.H2, Theme.InkSoft, TextAlignmentOptions.Center, false, "Status");
            UiKit.Place(status.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(1800f, 300f));
        }

        public override void OnEnter(object arg) => Load();

        public override void OnBack() => Context.Router.Show(AppScreenId.Mode, remember: false);

        void Load()
        {
            var my = ++generation;
            foreach (var c in cards) UiKit.Kill(c);
            cards.Clear();
            status.text = AppText.CommunityLoading;
            status.gameObject.SetActive(true);
            Context.Share.List((list, reason) =>
            {
                if (my != generation || this == null) return;
                if (list == null)
                {
                    status.text = AppText.CommunityFailed;
                    return;
                }
                status.gameObject.SetActive(list.Length == 0);
                status.text = AppText.CommunityEmpty;
                for (var i = 0; i < list.Length && i < MaxCards; i++)
                {
                    cards.Add(BuildCard(list[i], my));
                }
            });
        }

        GameObject BuildCard(StageShareClient.Summary summary, int my)
        {
            var card = UiKit.Panel(content, "Community", Theme.Card, Theme.CardRadius, shadow: true);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, 0f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.None;
            var id = summary.id;
            button.onClick.AddListener(() =>
            {
                UiSound.Play(UiSound.Cue.Click);
                Play(id);
            });
            card.gameObject.AddComponent<PressScale>();

            var name = UiKit.FitLabel(card.rectTransform, summary.name, Theme.H2, Theme.Ink, TextAlignmentOptions.Left, true, "Name");
            UiKit.Area(name.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 0.98f), 26f);
            var map = UiKit.NewRect("Map", card.rectTransform);
            UiKit.Area(map, new Vector2(0f, 0.36f), new Vector2(1f, 0.85f), 24f);

            var info = string.Format(AppText.CommunityBoxes, summary.boxCount) + (summary.hasRules ? "　" + AppText.CommunityRules : "");
            var infoLabel = UiKit.Label(card.rectTransform, info, Theme.Small, summary.hasRules ? Theme.Red : Theme.InkSoft, TextAlignmentOptions.Left, true, "Info");
            UiKit.Area(infoLabel.rectTransform, new Vector2(0f, 0.2f), new Vector2(1f, 0.32f), 26f);
            var best = CardList.Best(id);
            var bestLabel = UiKit.Label(card.rectTransform, best.HasValue ? string.Format(AppText.CommunityBest, FlowRules.FormatTime(best.Value)) : AppText.CommunityNoBest,
                Theme.Small, Theme.InkSoft, TextAlignmentOptions.Left, false, "Best");
            UiKit.Area(bestLabel.rectTransform, new Vector2(0f, 0.06f), new Vector2(1f, 0.18f), 26f);

            // 図は、1 件を読んでから描く
            Fetch(id, stage =>
            {
                if (my != generation || map == null || stage == null) return;
                Canvas.ForceUpdateCanvases();
                StageMiniMap.Draw(map, stage);
            });
            return card.gameObject;
        }

        void Fetch(string id, System.Action<StageData> done)
        {
            if (loaded.TryGetValue(id, out var cached))
            {
                done(cached);
                return;
            }
            Context.Share.Get(id, (stage, reason) =>
            {
                if (stage != null) loaded[id] = stage;
                done(stage);
            });
        }

        void Play(string id)
        {
            Fetch(id, stage =>
            {
                if (stage == null)
                {
                    UiSound.Play(UiSound.Cue.Error);
                    Context.Overlays.Toast(AppText.PlayLoadFailed, AppOverlays.ToastKind.Bad);
                    return;
                }
                Context.Flow.PlayStageData(stage, StageSource.Community);
            });
        }
    }
}
