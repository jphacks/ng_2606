using System;
using System.Collections.Generic;
using DeskStage.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// キャラを えらぶ。あそぶときに立つキャラを、人形の写真から作って、選ぶ。
    ///  ・いちらん：「いつもの こ」と、撮ったキャラのカード。押すと選ぶ。「あたらしく とる」「えらんだ こを けす」
    ///  ・撮る：人型の枠に人形を合わせて、シャッター
    ///  ・たしかめ：切り抜いた絵を見て、「これに する」か「とりなおす」
    /// 撮ったキャラの保存と、選んだキャラの記録は、CharacterStore。
    /// </summary>
    public class CharactersScreen : AppScreen
    {
        CharacterStore store;
        CharacterCapture camera;

        RectTransform listView;
        RectTransform captureView;
        RectTransform reviewView;

        RectTransform tileRow;
        AppButton deleteButton;

        UiParts.PreviewFrame preview;
        RawImage guide;
        Texture2D guideTexture;
        float guideAspect;
        TMP_Text captureHint;
        UnityEngine.UI.Button shutter;

        RawImage reviewImage;
        AspectRatioFitter reviewFitter;
        TMP_Text reviewText;
        AppButton reviewOk;

        CutoutResult pending;
        Texture2D pendingTexture;
        readonly List<UnityEngine.Object> tileObjects = new List<UnityEngine.Object>();

        protected override void Build()
        {
            store = CharacterStore.AtDefaultPlace();
            camera = Context.Camera;
            BuildList();
            BuildCapture();
            BuildReview();
        }

        // ---- 画面の部品 ----

        void BuildList()
        {
            listView = UiKit.Fill(UiKit.NewRect("List", Rect));
            UiKit.Background(listView, "Back", Theme.Cream, Theme.Hex("FFE9C2"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", listView));

            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(340f, Theme.SmallButtonHeight),
                () => OnBack(), "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);
            var title = UiKit.Label(safe, AppText.CharTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1400f, 120f));

            var scrollRect = UiKit.NewRect("Scroll", safe);
            UiKit.Area(scrollRect, new Vector2(0f, 0.2f), new Vector2(1f, 0.8f));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-20f, -50f, -20f, -30f);
            scrollRect.gameObject.AddComponent<Image>().color = Color.clear;

            tileRow = UiKit.NewRect("Tiles", scrollRect);
            tileRow.anchorMin = new Vector2(0f, 0f);
            tileRow.anchorMax = new Vector2(0f, 1f);
            tileRow.pivot = new Vector2(0f, 0.5f);
            tileRow.anchoredPosition = Vector2.zero;
            var layout = tileRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.padding = new RectOffset((int)Theme.Margin, (int)Theme.Margin, 20, 30);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            tileRow.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = tileRow;

            var take = UiKit.Button(safe, AppText.CharNew, UiKit.ButtonStyle.Primary, new Vector2(700f, Theme.ButtonHeight),
                ShowCapture, "New", Theme.H2);
            UiKit.Place(take.Rect, new Vector2(0.5f, 0f), new Vector2(-250f, 40f), take.Rect.sizeDelta);
            deleteButton = UiKit.Button(safe, AppText.CharDelete, UiKit.ButtonStyle.Soft, new Vector2(700f, Theme.ButtonHeight),
                OnDelete, "Delete", Theme.Body);
            UiKit.Place(deleteButton.Rect, new Vector2(0.5f, 0f), new Vector2(480f, 40f), deleteButton.Rect.sizeDelta);
            deleteButton.Label.color = Theme.Red;
        }

        void BuildCapture()
        {
            captureView = UiKit.Fill(UiKit.NewRect("Capture", Rect));
            captureView.gameObject.AddComponent<Image>().color = Theme.Darken(Theme.Navy, 0.35f);

            var left = UiKit.NewRect("Left", captureView);
            UiKit.Area(left, new Vector2(0f, 0f), new Vector2(0.62f, 1f));
            preview = UiParts.PreviewFrame.Create(left, "Preview");
            UiKit.Fill(preview.Rect);
            var guideRect = UiKit.Fill(UiKit.NewRect("Guide", preview.Rect));
            guide = guideRect.gameObject.AddComponent<RawImage>();
            guide.raycastTarget = false;
            guide.enabled = false;

            var panel = UiKit.NewRect("Panel", captureView);
            UiKit.Area(panel, new Vector2(0.62f, 0f), new Vector2(1f, 1f));
            panel.gameObject.AddComponent<Image>().sprite = UiKit.GradientSprite(Theme.Hex("2A3760"), Theme.Navy);
            var safe = UiKit.Fill(UiKit.NewRect("Safe", panel));
            captureHint = UiKit.Label(safe, AppText.CharCaptureHint, 48f, Color.white, TextAlignmentOptions.Center, true, "Hint");
            captureHint.textWrappingMode = TMPro.TextWrappingModes.NoWrap; // 改行は、文の中の \n だけ（勝手に折り返して、1 文字だけ次の行に落ちないように）
            UiKit.Place(captureHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(880f, 330f));
            shutter = UiParts.Shutter(safe, OnTake, 260f);
            UiKit.Place(shutter.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(260f, 260f));
            var cancel = UiKit.Button(safe, AppText.No, UiKit.ButtonStyle.Soft, new Vector2(460f, Theme.SmallButtonHeight),
                () => ShowList(), "Cancel", Theme.Body);
            UiKit.Place(cancel.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 90f), cancel.Rect.sizeDelta);
        }

        void BuildReview()
        {
            reviewView = UiKit.Fill(UiKit.NewRect("Review", Rect));
            UiKit.Background(reviewView, "Back", Theme.Cream, Theme.Hex("FFE9C2"));
            var safe = UiKit.Fill(UiKit.NewRect("Safe", reviewView));

            var checker = UiKit.Panel(safe, "Checker", Theme.Hex("D6DBE6"), Theme.CardRadius, shadow: true);
            UiKit.Place(checker.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 900f));
            var imageRect = UiKit.Fill(UiKit.NewRect("Image", checker.rectTransform));
            imageRect.offsetMin = new Vector2(30f, 30f);
            imageRect.offsetMax = new Vector2(-30f, -30f);
            reviewImage = imageRect.gameObject.AddComponent<RawImage>();
            reviewImage.raycastTarget = false;
            reviewFitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            reviewFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            reviewText = UiKit.Label(safe, "", Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Text");
            UiKit.Place(reviewText.rectTransform, new Vector2(0.72f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 300f));
            reviewOk = UiKit.Button(safe, AppText.CharReviewOk, UiKit.ButtonStyle.Primary, new Vector2(800f, Theme.ButtonHeight),
                OnConfirm, "Ok", Theme.H1);
            UiKit.Place(reviewOk.Rect, new Vector2(0.72f, 0.5f), new Vector2(0f, -60f), reviewOk.Rect.sizeDelta);
            var retake = UiKit.Button(safe, AppText.CharRetake, UiKit.ButtonStyle.Soft, new Vector2(800f, Theme.SmallButtonHeight),
                ShowCapture, "Retake", Theme.Body);
            UiKit.Place(retake.Rect, new Vector2(0.72f, 0.5f), new Vector2(0f, -250f), retake.Rect.sizeDelta);
        }

        // ---- 切り替え ----

        public override void OnEnter(object arg) => ShowList();

        public override void OnExit()
        {
            if (camera != null) camera.Close();
            ReleasePending();
            ReleaseTiles();
            if (guideTexture != null)
            {
                UiKit.Kill(guideTexture);
                guideTexture = null;
                guideAspect = 0f;
            }
        }

        public override void OnBack()
        {
            if (captureView.gameObject.activeSelf || reviewView.gameObject.activeSelf)
            {
                ShowList();
                return;
            }
            Context.Router.Show(AppScreenId.Home);
        }

        void Only(RectTransform view)
        {
            listView.gameObject.SetActive(view == listView);
            captureView.gameObject.SetActive(view == captureView);
            reviewView.gameObject.SetActive(view == reviewView);
        }

        void ShowList()
        {
            if (camera != null) camera.Close();
            ReleasePending();
            Only(listView);
            RebuildTiles();
        }

        void ShowCapture()
        {
            ReleasePending();
            Only(captureView);
            captureHint.text = AppText.CharCaptureHint;
            if (camera != null) camera.Open();
        }

        void ShowReview(CutoutResult result)
        {
            if (camera != null) camera.Close();
            ReleasePending();
            Only(reviewView);
            pending = result;
            if (result.Ok)
            {
                pendingTexture = new Texture2D(result.Width, result.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                pendingTexture.SetPixels32(result.Pixels);
                pendingTexture.Apply();
                reviewImage.texture = pendingTexture;
                reviewImage.enabled = true;
                reviewFitter.aspectRatio = (float)result.Width / result.Height;
                reviewText.text = AppText.CharReviewAsk;
                reviewOk.gameObject.SetActive(true);
            }
            else
            {
                reviewImage.enabled = false;
                reviewText.text = AppText.CharCutFailed;
                reviewOk.gameObject.SetActive(false);
            }
        }

        // ---- 動き ----

        void Update()
        {
            if (camera == null || !captureView.gameObject.activeSelf)
            {
                return;
            }
            var texture = camera.Preview;
            preview.Show(texture);
            shutter.interactable = texture != null;
            guide.enabled = texture != null;
            if (texture != null && texture.height > 0)
            {
                UpdateGuide((float)texture.width / texture.height);
            }
            if (!string.IsNullOrEmpty(camera.Problem))
            {
                captureHint.text = camera.Problem;
            }
        }

        void UpdateGuide(float aspect)
        {
            if (guideTexture != null && Mathf.Abs(aspect - guideAspect) < 0.001f)
            {
                return;
            }
            const int guideHeight = 240;
            var width = Mathf.Max(8, Mathf.RoundToInt(guideHeight * aspect));
            if (guideTexture != null) UiKit.Kill(guideTexture);
            var region = HumanMask.CropRegion(aspect);
            guideTexture = new Texture2D(width, guideHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            guideTexture.SetPixels32(HumanMask.GuideOverlay(width, guideHeight, region));
            guideTexture.Apply();
            guide.texture = guideTexture;
            guideAspect = aspect;
        }

        void OnTake()
        {
            if (camera == null) return;
            UiSound.Play(UiSound.Cue.Shutter);
            ShowReview(camera.TakeFramed());
        }

        void OnConfirm()
        {
            if (!pending.Ok || pendingTexture == null)
            {
                return;
            }
            var info = store.Add("", pendingTexture.EncodeToPNG(), (float)pending.Width / pending.Height, DateTimeOffset.Now);
            ReleasePending();
            ShowList();
            if (info == null)
            {
                UiSound.Play(UiSound.Cue.Error);
                Context.Overlays.Toast(AppText.CheckSaveFailed, AppOverlays.ToastKind.Bad);
                return;
            }
            store.SelectedId = info.id; // 撮ったキャラを、そのまま選ぶ
            RebuildTiles();
            UiSound.Play(UiSound.Cue.Success);
            Context.Overlays.Toast(AppText.CharChosen, AppOverlays.ToastKind.Good);
        }

        void OnDelete()
        {
            var id = store.SelectedId;
            if (string.IsNullOrEmpty(id))
            {
                UiSound.Play(UiSound.Cue.Error);
                Context.Overlays.Toast(AppText.CharCannotDeleteDefault, AppOverlays.ToastKind.Bad);
                return;
            }
            Context.Overlays.Confirm(AppText.CharDeleteConfirm, () =>
            {
                var ok = store.Delete(id);
                RebuildTiles();
                Context.Overlays.Toast(ok ? AppText.CharDeleted : AppText.CheckSaveFailed, ok ? AppOverlays.ToastKind.Info : AppOverlays.ToastKind.Bad);
            }, AppText.HomeDelete, AppText.No, true);
        }

        void Select(string id)
        {
            store.SelectedId = id;
            RebuildTiles();
            Context.Overlays.Toast(AppText.CharChosen, AppOverlays.ToastKind.Good, 1.4f);
        }

        // ---- カード ----

        void RebuildTiles()
        {
            ReleaseTiles();
            foreach (Transform child in tileRow)
            {
                UiKit.Kill(child.gameObject);
            }
            var selected = store.SelectedId;
            AddTile(null, AppText.CharDefault, string.IsNullOrEmpty(selected));
            foreach (var info in store.List())
            {
                AddTile(info, string.IsNullOrEmpty(info.name) ? AppText.CharNamed : info.name, info.id == selected);
            }
            deleteButton.Interactable = !string.IsNullOrEmpty(selected);
        }

        void AddTile(DeskStage.Merge.CharacterInfo info, string label, bool isSelected)
        {
            const float width = 420f;
            var outer = UiKit.Panel(tileRow, "Tile", isSelected ? Theme.Orange : Color.white, Theme.CardRadius, shadow: true);
            outer.rectTransform.sizeDelta = new Vector2(width, 0f);
            var le = outer.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            var button = outer.gameObject.AddComponent<Button>();
            button.targetGraphic = outer;
            button.transition = Selectable.Transition.None;
            var id = info != null ? info.id : "";
            button.onClick.AddListener(() =>
            {
                UiSound.Play(UiSound.Cue.Click);
                Select(id);
            });
            outer.gameObject.AddComponent<PressScale>();

            var inner = UiKit.Panel(outer.rectTransform, "Inner", Color.white, Theme.CardRadius - 8f);
            UiKit.Fill(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(10f, 10f);
            inner.rectTransform.offsetMax = new Vector2(-10f, -10f);
            inner.raycastTarget = false;

            var art = UiKit.Panel(inner.rectTransform, "Art", Theme.SkyLight, 30f);
            UiKit.Area(art.rectTransform, new Vector2(0f, 0.22f), new Vector2(1f, 1f), 18f);
            art.raycastTarget = false;

            if (info != null)
            {
                var png = store.LoadPng(info.id);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                if (png != null && texture.LoadImage(png))
                {
                    tileObjects.Add(texture);
                    var rect = UiKit.Fill(UiKit.NewRect("Image", art.rectTransform));
                    rect.offsetMin = new Vector2(16f, 16f);
                    rect.offsetMax = new Vector2(-16f, -16f);
                    var raw = rect.gameObject.AddComponent<RawImage>();
                    raw.texture = texture;
                    raw.raycastTarget = false;
                    var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = info.aspect > 0f ? info.aspect : (float)texture.width / texture.height;
                }
                else
                {
                    UiKit.Kill(texture);
                }
            }
            else
            {
                DrawDefault(art.rectTransform);
            }

            var text = UiKit.FitLabel(inner.rectTransform, label, Theme.Body, Theme.Ink, TextAlignmentOptions.Center, true, "Label");
            UiKit.Area(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.22f), 10f);

            if (isSelected)
            {
                var badge = UiKit.Disc(outer.rectTransform, "Chosen", Theme.Green, 96f);
                UiKit.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(-14f, -14f), badge.rectTransform.sizeDelta);
                var mark = UiKit.Label(badge.rectTransform, "OK", 42f, Color.white, TextAlignmentOptions.Center, true, "Mark");
                UiKit.Fill(mark.rectTransform);
            }
        }

        // 「いつもの こ」の絵（青いカプセルのキャラ）
        static void DrawDefault(RectTransform art)
        {
            var body = UiKit.Panel(art, "Body", Theme.Blue, 80f);
            UiKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 250f));
            for (var i = 0; i < 2; i++)
            {
                var eye = UiKit.Disc(body.rectTransform, "Eye" + i, Color.white, 52f);
                UiKit.Place(eye.rectTransform, new Vector2(0.3f + i * 0.4f, 0.68f), Vector2.zero, eye.rectTransform.sizeDelta);
                var pupil = UiKit.Disc(eye.rectTransform, "Pupil", Theme.Ink, 24f);
                UiKit.Place(pupil.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, pupil.rectTransform.sizeDelta);
            }
        }

        void ReleasePending()
        {
            if (pendingTexture != null)
            {
                UiKit.Kill(pendingTexture);
                pendingTexture = null;
            }
            pending = default;
        }

        void ReleaseTiles()
        {
            foreach (var o in tileObjects)
            {
                if (o != null) UiKit.Kill(o);
            }
            tileObjects.Clear();
        }
    }
}
