using DeskStage.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// ステージの はいけい。ステージの うしろに 見せる絵を、用意した絵（BackdropPresets）から選ぶ。
    ///  ・左：いまの はいけい（無ければ、その案内）
    ///  ・右：絵の一覧と「なくす」
    /// 決めた絵は、つぎに作る ステージから、うしろに出る（BackdropStore に保存）。
    /// 写真で撮る機能（「あたらしく とる」）は、2026-11-10 に外した（使わないため）。前に撮った写真は、なくすまで残る。
    /// </summary>
    public class BackdropScreen : AppScreen
    {
        const int Quality = 85;
        const int PresetWidth = 1024;
        const int PresetHeight = 512;

        BackdropStore store;

        RectTransform statusView;

        RawImage currentImage;
        TMP_Text currentCaption;
        RectTransform presetGrid;
        readonly System.Collections.Generic.List<UnityEngine.Object> presetObjects = new System.Collections.Generic.List<UnityEngine.Object>();
        TMP_Text currentText;
        GameObject currentCard;
        AppButton clearButton;
        Texture2D currentTexture;

        protected override void Build()
        {
            store = BackdropStore.AtDefaultPlace();
            BuildStatus();
        }

        void BuildStatus()
        {
            statusView = UiKit.Fill(UiKit.NewRect("Status", Rect));
            UiKit.Background(statusView, "Back", Theme.Cream, Theme.Hex("FFE9C2"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", statusView));

            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(340f, Theme.SmallButtonHeight),
                () => OnBack(), "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);
            var title = UiKit.Label(safe, AppText.BackdropTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1400f, 120f));

            // 左：いまの はいけい（写真）か、無いときの案内
            var card = UiKit.Panel(safe, "Card", Color.white, Theme.CardRadius, shadow: true);
            currentCard = card.gameObject;
            UiKit.Place(card.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0f, 60f), new Vector2(880f, 560f));
            var imageRect = UiKit.Fill(UiKit.NewRect("Image", card.rectTransform));
            imageRect.offsetMin = new Vector2(20f, 20f);
            imageRect.offsetMax = new Vector2(-20f, -20f);
            currentImage = imageRect.gameObject.AddComponent<RawImage>();
            currentImage.raycastTarget = false;
            currentImage.gameObject.AddComponent<CoverUv>();
            currentText = UiKit.Label(safe, "", Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, false, "Text");
            UiKit.Place(currentText.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0f, 60f), new Vector2(880f, 300f));
            currentCaption = UiKit.Label(safe, "", Theme.Body, Theme.Ink, TextAlignmentOptions.Center, true, "Caption");
            UiKit.Place(currentCaption.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0f, -285f), new Vector2(880f, 80f));

            // 右：えらぶ（プリセットの絵）と、なくす
            var pick = UiKit.Label(safe, AppText.BackdropPick, Theme.H2, Theme.Ink, TextAlignmentOptions.Left, true, "Pick");
            UiKit.Place(pick.rectTransform, new Vector2(0.62f, 0.5f), new Vector2(0f, 280f), new Vector2(1000f, 90f));
            pick.rectTransform.pivot = new Vector2(0f, 0.5f);
            pick.rectTransform.anchorMin = pick.rectTransform.anchorMax = new Vector2(0.58f, 0.5f);
            pick.rectTransform.anchoredPosition = new Vector2(0f, 280f);

            presetGrid = UiKit.NewRect("Presets", safe);
            presetGrid.anchorMin = presetGrid.anchorMax = new Vector2(0.58f, 0.5f);
            presetGrid.pivot = new Vector2(0f, 1f);
            presetGrid.anchoredPosition = new Vector2(0f, 220f);
            presetGrid.sizeDelta = new Vector2(1000f, 460f);
            var grid = presetGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(310f, 210f);
            grid.spacing = new Vector2(30f, 30f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            clearButton = UiKit.Button(safe, AppText.BackdropClear, UiKit.ButtonStyle.Soft, new Vector2(500f, Theme.ButtonHeight),
                OnClear, "Clear", Theme.H2);
            UiKit.Place(clearButton.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 40f), clearButton.Rect.sizeDelta);
            clearButton.Label.color = Theme.Red;
        }

        // ---- プリセット ----

        const string PresetKey = "app.backdrop.preset";

        static string CurrentPreset => PlayerPrefs.GetString(PresetKey, "");

        void RebuildPresets()
        {
            foreach (var o in presetObjects)
            {
                if (o != null) UiKit.Kill(o);
            }
            presetObjects.Clear();
            foreach (Transform child in presetGrid)
            {
                UiKit.Kill(child.gameObject);
            }
            var current = CurrentPreset;
            foreach (var preset in BackdropPresets.All)
            {
                AddPresetTile(preset, preset.Id == current);
            }
        }

        void AddPresetTile(BackdropPresets.Preset preset, bool isCurrent)
        {
            var outer = UiKit.Panel(presetGrid, "Preset_" + preset.Id, isCurrent ? Theme.Orange : Color.white, 40f, shadow: true);
            var button = outer.gameObject.AddComponent<Button>();
            button.targetGraphic = outer;
            button.transition = Selectable.Transition.None;
            var id = preset.Id;
            button.onClick.AddListener(() =>
            {
                UiSound.Play(UiSound.Cue.Click);
                ChoosePreset(id);
            });
            outer.gameObject.AddComponent<PressScale>();

            var texture = new Texture2D(192, 96, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear };
            texture.SetPixels32(BackdropPresets.Render(preset.Id, 192, 96));
            texture.Apply();
            presetObjects.Add(texture);
            var picture = UiKit.Fill(UiKit.NewRect("Picture", outer.rectTransform));
            picture.offsetMin = new Vector2(10f, 62f);
            picture.offsetMax = new Vector2(-10f, -10f);
            var raw = picture.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            picture.gameObject.AddComponent<CoverUv>();

            var label = UiKit.FitLabel(outer.rectTransform, preset.Name, Theme.Body * 0.9f, isCurrent ? Theme.Ink : Theme.Ink, TextAlignmentOptions.Center, true, "Name");
            UiKit.Fill(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(8f, 4f);
            label.rectTransform.offsetMax = new Vector2(-8f, -150f);
        }

        // プリセットの絵を、写真と同じ形（JPEG）にして、背景として保存する
        void ChoosePreset(string id)
        {
            var pixels = BackdropPresets.Render(id, PresetWidth, PresetHeight);
            if (pixels == null)
            {
                return;
            }
            var texture = new Texture2D(PresetWidth, PresetHeight, TextureFormat.RGB24, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            var jpeg = texture.EncodeToJPG(Quality);
            UiKit.Kill(texture);
            var ok = store.Save(jpeg);
            if (ok) PlayerPrefs.SetString(PresetKey, id);
            ShowStatus();
            UiSound.Play(ok ? UiSound.Cue.Success : UiSound.Cue.Error);
            Context.Overlays.Toast(ok ? AppText.BackdropSaved : AppText.CheckSaveFailed, ok ? AppOverlays.ToastKind.Good : AppOverlays.ToastKind.Bad, 3.2f);
        }

        // ---- 切り替え ----

        public override void OnEnter(object arg) => ShowStatus();

        public override void OnExit()
        {
            ReleaseCurrent();
            foreach (var o in presetObjects)
            {
                if (o != null) UiKit.Kill(o);
            }
            presetObjects.Clear();
        }

        public override void OnBack() => Context.Router.Show(AppScreenId.Home);

        void ShowStatus()
        {
            ReleaseCurrent();
            var jpeg = store.Load();
            if (jpeg != null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (texture.LoadImage(jpeg))
                {
                    currentTexture = texture;
                    currentImage.texture = texture;
                    currentImage.GetComponent<CoverUv>().Apply();
                }
                else
                {
                    UiKit.Kill(texture);
                }
            }
            var has = currentTexture != null;
            currentCard.SetActive(has);
            currentText.text = has ? "" : AppText.BackdropNone;
            currentText.gameObject.SetActive(!has);
            clearButton.Interactable = has;
            var preset = CurrentPreset;
            var presetName = "";
            foreach (var p in BackdropPresets.All)
            {
                if (p.Id == preset) presetName = p.Name;
            }
            currentCaption.text = !has ? "" : presetName.Length > 0 ? string.Format(AppText.BackdropCurrentPreset, presetName) : AppText.BackdropCurrentPhoto;
            RebuildPresets();
        }

        void OnClear()
        {
            Context.Overlays.Confirm(AppText.BackdropClearConfirm, () =>
            {
                var ok = store.Clear();
                if (ok) PlayerPrefs.DeleteKey(PresetKey);
                ShowStatus();
                Context.Overlays.Toast(ok ? AppText.BackdropCleared : AppText.CheckSaveFailed, ok ? AppOverlays.ToastKind.Info : AppOverlays.ToastKind.Bad);
            }, AppText.BackdropClear, AppText.No, true);
        }

        void ReleaseCurrent()
        {
            if (currentTexture != null)
            {
                UiKit.Kill(currentTexture);
                currentTexture = null;
            }
        }
    }
}
