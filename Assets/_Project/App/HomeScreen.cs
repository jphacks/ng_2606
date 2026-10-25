using System.Collections.Generic;
using System.IO;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// ホーム（ステージいちらん）。いちばん左に「あたらしく つくる」、そのとなりに、保存したステージのカードが並ぶ（横にスクロール）。
    /// カードを押すと選ぶ。選ぶと、下に「あそぶ」「けす」が出る。右上に「キャラ」「はいけい」。
    /// ステージの一覧は、B の StageContext.StageList（StageListChanged で更新）。絵は、保存先の thumbnail.jpg（無ければ、積み木の絵）。
    /// </summary>
    public class HomeScreen : AppScreen
    {
        const float CardWidth = 520f;
        const float CardSpacing = 44f;

        RectTransform content;
        TMP_Text emptyLabel;
        RectTransform actionBar;
        TMP_Text actionHint;
        AppButton playButton;
        AppButton deleteButton;

        string selectedId = "";
        readonly Dictionary<string, Texture2D> thumbnails = new Dictionary<string, Texture2D>();
        readonly List<GameObject> cards = new List<GameObject>();

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Cream, Theme.Hex("FFE9C2"));

            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            // 上の帯：タイトルと、キャラ・はいけい
            var title = UiKit.Label(safe, AppText.HomeTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Left, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(Theme.Margin, -50f), new Vector2(1300f, 120f));

            var backdrop = UiKit.Button(safe, AppText.HomeBackdrop, UiKit.ButtonStyle.Soft, new Vector2(380f, Theme.SmallButtonHeight),
                () => Context.Flow.OpenBackdrop(), "Backdrop", Theme.Body);
            UiKit.Place(backdrop.Rect, new Vector2(1f, 1f), new Vector2(-Theme.Margin, -45f), backdrop.Rect.sizeDelta);
            var characters = UiKit.Button(safe, AppText.HomeCharacters, UiKit.ButtonStyle.Soft, new Vector2(380f, Theme.SmallButtonHeight),
                () => Context.Flow.OpenCharacters(), "Characters", Theme.Body);
            UiKit.Place(characters.Rect, new Vector2(1f, 1f), new Vector2(-Theme.Margin - 420f, -45f), characters.Rect.sizeDelta);

            // 真ん中：カードの列（横にスクロール）
            var scrollRect = UiKit.NewRect("Scroll", safe);
            UiKit.Area(scrollRect, new Vector2(0f, 0.17f), new Vector2(1f, 0.8f));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scrollRect.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-20f, -50f, -20f, -30f); // 影と、押して縮む分まで見せる
            scrollRect.gameObject.AddComponent<Image>().color = Color.clear; // 空いている所でも、スクロールの指を受ける

            content = UiKit.NewRect("Content", scrollRect);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = CardSpacing;
            layout.padding = new RectOffset((int)Theme.Margin, (int)Theme.Margin, 24, 40);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            emptyLabel = UiKit.Label(safe, AppText.HomeEmpty, Theme.Body, Theme.InkSoft, TextAlignmentOptions.Left, false, "Empty");
            UiKit.Place(emptyLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(Theme.Margin + CardWidth + 80f, 0f), new Vector2(1200f, 220f));

            // 下の帯：選んだステージの操作
            actionBar = UiKit.NewRect("ActionBar", safe);
            UiKit.Area(actionBar, new Vector2(0f, 0f), new Vector2(1f, 0.17f));
            actionHint = UiKit.Label(actionBar, AppText.HomeSelectHint, Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, false, "Hint");
            UiKit.Fill(actionHint.rectTransform);
            playButton = UiKit.Button(actionBar, AppText.HomePlay, UiKit.ButtonStyle.Primary, new Vector2(640f, Theme.ButtonHeight),
                () => Context.Flow.PlayStage(selectedId), "Play", Theme.H1);
            UiKit.Place(playButton.Rect, new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f), playButton.Rect.sizeDelta);
            deleteButton = UiKit.Button(actionBar, AppText.HomeDelete, UiKit.ButtonStyle.Soft, new Vector2(400f, Theme.ButtonHeight),
                OnDelete, "Delete", Theme.H2);
            UiKit.Place(deleteButton.Rect, new Vector2(0.5f, 0.5f), new Vector2(430f, 0f), deleteButton.Rect.sizeDelta);
            deleteButton.Label.color = Theme.Red;
        }

        public override void OnEnter(object arg)
        {
            StageContext.StageListChanged += Refresh;
            selectedId = "";
            Refresh();
        }

        public override void OnExit()
        {
            StageContext.StageListChanged -= Refresh;
            ReleaseThumbnails();
        }

        public override void OnBack() => Context.Router.Show(AppScreenId.Title, remember: false);

        /// <summary>選んでいるステージ（無ければ空）。</summary>
        public string SelectedId => selectedId;

        void OnDelete()
        {
            if (string.IsNullOrEmpty(selectedId))
            {
                return;
            }
            var id = selectedId;
            Context.Overlays.Confirm(AppText.HomeDeleteConfirm, () =>
            {
                Context.Flow.DeleteStage(id);
                selectedId = "";
            }, AppText.HomeDelete, AppText.No, danger: true);
        }

        void Refresh()
        {
            foreach (var card in cards)
            {
                UiKit.Kill(card);
            }
            cards.Clear();
            ReleaseThumbnails();

            var height = ((RectTransform)content.parent).rect.height - 64f;
            cards.Add(BuildNewCard());

            var list = StageContext.StageList;
            var any = false;
            if (list != null)
            {
                foreach (var summary in list)
                {
                    cards.Add(BuildStageCard(summary));
                    any = true;
                }
            }
            emptyLabel.gameObject.SetActive(!any);

            if (!string.IsNullOrEmpty(selectedId) && !Contains(list, selectedId))
            {
                selectedId = "";
            }
            RefreshActionBar();
            if (height < 0f) return;
        }

        static bool Contains(IReadOnlyList<StageSummary> list, string id)
        {
            if (list == null) return false;
            foreach (var s in list) if (s.id == id) return true;
            return false;
        }

        // ---- カード ----

        GameObject BuildNewCard()
        {
            var card = UiKit.Panel(content, "NewCard", Theme.Orange, Theme.CardRadius, shadow: true);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, 0f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                UiSound.Play(UiSound.Cue.Click);
                Context.Flow.StartNewStage();
            });
            card.gameObject.AddComponent<PressScale>();

            // 大きな「＋」（白い丸に、2 本の線）
            var disc = UiKit.Disc(card.rectTransform, "Plus", Color.white, 230f);
            UiKit.Place(disc.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, disc.rectTransform.sizeDelta);
            disc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            foreach (var size in new[] { new Vector2(110f, 26f), new Vector2(26f, 110f) })
            {
                var bar = UiKit.Panel(disc.rectTransform, "Bar", Theme.Orange, 13f);
                UiKit.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
                bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bar.raycastTarget = false;
            }

            var label = UiKit.FitLabel(card.rectTransform, AppText.HomeNew, Theme.H2, Theme.Ink, TextAlignmentOptions.Center, true);
            UiKit.Area(label.rectTransform, new Vector2(0f, 0.2f), new Vector2(1f, 0.36f), 24f);
            var hint = UiKit.Label(card.rectTransform, AppText.HomeNewHint, Theme.Small, Theme.Darken(Theme.Orange, 0.55f), TextAlignmentOptions.Center);
            UiKit.Area(hint.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.2f), 16f);
            return card.gameObject;
        }

        GameObject BuildStageCard(StageSummary summary)
        {
            var selected = summary.id == selectedId;
            var card = UiKit.Panel(content, "StageCard", selected ? Theme.Orange : Theme.Card, Theme.CardRadius, shadow: true);
            card.rectTransform.sizeDelta = new Vector2(CardWidth, 0f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;
            button.transition = Selectable.Transition.None;
            button.interactable = summary.loadable;
            var id = summary.id;
            button.onClick.AddListener(() =>
            {
                UiSound.Play(UiSound.Cue.Click);
                selectedId = id;
                Refresh();
            });
            card.gameObject.AddComponent<PressScale>();

            // 絵（保存した 1 枚目の写真。無ければ、積み木の絵）
            var inner = UiKit.Panel(card.rectTransform, "Frame", selected ? Color.white : Theme.SkyLight, Theme.CardRadius - 12f);
            UiKit.Area(inner.rectTransform, new Vector2(0f, 0.34f), new Vector2(1f, 1f), 16f);
            inner.raycastTarget = false;
            var texture = LoadThumbnail(summary.id);
            if (texture != null)
            {
                var photoRect = UiKit.Fill(UiKit.NewRect("Photo", inner.rectTransform));
                photoRect.offsetMin = new Vector2(8f, 8f);
                photoRect.offsetMax = new Vector2(-8f, -8f);
                var raw = photoRect.gameObject.AddComponent<RawImage>();
                raw.texture = texture;
                raw.raycastTarget = false;
                photoRect.gameObject.AddComponent<CoverUv>().Apply();
            }
            else
            {
                BuildPlaceholderBlocks(inner.rectTransform);
            }

            var name = UiKit.FitLabel(card.rectTransform, summary.name, Theme.H2, Theme.Ink, TextAlignmentOptions.Left, true);
            UiKit.Area(name.rectTransform, new Vector2(0f, 0.17f), new Vector2(1f, 0.32f), 22f);
            var sub = summary.loadable ? FormatDate(summary.createdAt) : AppText.HomeUnreadable;
            var date = UiKit.Label(card.rectTransform, sub, Theme.Small, selected ? Theme.Ink : Theme.InkSoft, TextAlignmentOptions.Left);
            UiKit.Area(date.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.17f), 22f);
            if (summary.id.StartsWith("sample"))
            {
                var badge = UiKit.Panel(card.rectTransform, "Badge", Theme.Green, Theme.ButtonRadius);
                UiKit.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(220f, 70f));
                badge.rectTransform.pivot = new Vector2(1f, 1f);
                var text = UiKit.FitLabel(badge.rectTransform, AppText.HomeSample, Theme.Small, Color.white, TextAlignmentOptions.Center, true);
                UiKit.Fill(text.rectTransform);
            }
            return card.gameObject;
        }

        /// <summary>写真が無いときの、積み木の絵（色のついた四角が 3 つ）。</summary>
        static void BuildPlaceholderBlocks(RectTransform parent)
        {
            var sizes = new[] { new Vector2(150f, 150f), new Vector2(120f, 200f), new Vector2(170f, 100f) };
            var xs = new[] { 0.25f, 0.52f, 0.78f };
            for (var i = 0; i < 3; i++)
            {
                var block = UiKit.Panel(parent, "Block", Theme.BlockColors[(i * 2) % Theme.BlockColors.Length], 24f);
                var rect = block.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(xs[i], 0.18f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = sizes[i];
                block.raycastTarget = false;
            }
        }

        // ---- 選んだときの操作 ----

        void RefreshActionBar()
        {
            var has = !string.IsNullOrEmpty(selectedId);
            actionHint.gameObject.SetActive(!has);
            playButton.gameObject.SetActive(has);
            deleteButton.gameObject.SetActive(has);
        }

        // ---- 絵の読み込み ----

        Texture2D LoadThumbnail(string id)
        {
            if (thumbnails.TryGetValue(id, out var cached))
            {
                return cached;
            }
            Texture2D texture = null;
            try
            {
                var path = Path.Combine(Application.persistentDataPath, "Stages", id, "thumbnail.jpg");
                if (File.Exists(path))
                {
                    var t = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (t.LoadImage(File.ReadAllBytes(path)))
                    {
                        texture = t;
                    }
                    else
                    {
                        UiKit.Kill(t);
                    }
                }
            }
            catch (IOException)
            {
            }
            thumbnails[id] = texture;
            return texture;
        }

        void ReleaseThumbnails()
        {
            foreach (var t in thumbnails.Values)
            {
                UiKit.Kill(t);
            }
            thumbnails.Clear();
        }

        static string FormatDate(string iso)
        {
            return System.DateTimeOffset.TryParse(iso, out var date) ? date.ToLocalTime().ToString("M/d HH:mm") : "";
        }
    }
}
