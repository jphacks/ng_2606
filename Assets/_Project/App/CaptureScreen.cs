using DeskStage.AR;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// 撮影。左にカメラの映像（切り取らずに全体）、右に、手順の札・案内・シャッター。
    /// 1 枚目は真上、2 枚目は斜め。2 枚そろうと「つくっている」へ進む（流れは AppFlow）。
    /// うまくいかなかったときは、この画面に戻ってきて、原因に合う文と「もういちど」を出す（arg に FailureKind）。
    /// </summary>
    public class CaptureScreen : AppScreen
    {
        UiParts.PreviewFrame preview;
        UiParts.StepChip chip1;
        UiParts.StepChip chip2;
        TMP_Text hint;
        TMP_Text warning;
        UnityEngine.UI.Button shutter;
        CanvasGroup shutterGroup;
        RectTransform preparing;
        Image flash;

        RectTransform failure;
        TMP_Text failureText;
        AppButton failureRetry;
        AppButton failureSettings;

        PhotoCapture photo;
        bool hadTop;
        ScanStatus lastStatus;
        float flashTime = 1f;
        bool failed;

        protected override void Build()
        {
            var bg = Rect.gameObject.AddComponent<Image>();
            bg.color = Theme.Darken(Theme.Navy, 0.35f);

            // 左：カメラの映像
            var left = UiKit.NewRect("Left", Rect);
            UiKit.Area(left, new Vector2(0f, 0f), new Vector2(0.62f, 1f));
            preview = UiParts.PreviewFrame.Create(left, "Preview");
            UiKit.Fill(preview.Rect);
            UiParts.Grid(preview.Rect);

            preparing = UiKit.NewRect("Preparing", left);
            UiKit.Fill(preparing);
            var spin = UiKit.Spinner(preparing, "Spinner", Color.white, 150f);
            UiKit.Place(spin.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(150f, 150f));
            var prep = UiKit.Label(preparing, AppText.CapturePreparing, Theme.Body, Color.white, TextAlignmentOptions.Center, false, "Text");
            UiKit.Place(prep.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(900f, 100f));

            var back = UiKit.Button(Rect, AppText.Back, UiKit.ButtonStyle.Overlay, new Vector2(330f, Theme.SmallButtonHeight),
                () => OnBack(), "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -45f), back.Rect.sizeDelta);

            // 右：手順と、シャッター
            var panel = UiKit.NewRect("Panel", Rect);
            UiKit.Area(panel, new Vector2(0.62f, 0f), new Vector2(1f, 1f));
            var panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.sprite = UiKit.GradientSprite(Theme.Hex("2A3760"), Theme.Navy);

            var safe = UiKit.Fill(UiKit.NewRect("Safe", panel));
            chip1 = UiParts.StepChip.Create(safe, "Chip1", 1, AppText.CaptureStep1, new Vector2(400f, 120f));
            UiKit.Place(chip1.Rect, new Vector2(0.27f, 1f), new Vector2(0f, -70f), chip1.Rect.sizeDelta);
            chip2 = UiParts.StepChip.Create(safe, "Chip2", 2, AppText.CaptureStep2, new Vector2(400f, 120f));
            UiKit.Place(chip2.Rect, new Vector2(0.73f, 1f), new Vector2(0f, -70f), chip2.Rect.sizeDelta);

            hint = UiKit.Label(safe, "", 48f, Color.white, TextAlignmentOptions.Center, true, "Hint");
            hint.textWrappingMode = TMPro.TextWrappingModes.NoWrap; // 改行は、文の中の \n だけ（勝手に折り返して、1 文字だけ次の行に落ちないように）
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(880f, 260f));
            warning = UiKit.Label(safe, "", Theme.Body, Theme.Orange, TextAlignmentOptions.Center, true, "Warning");
            UiKit.Place(warning.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(880f, 110f));

            shutter = UiParts.Shutter(safe, OnShutter, 260f);
            UiKit.Place(shutter.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(260f, 260f));
            shutterGroup = shutter.gameObject.AddComponent<CanvasGroup>();

            flash = UiParts.Clear(Rect, "Flash", false);
            flash.color = new Color(1f, 1f, 1f, 0f);

            BuildFailure();
        }

        void BuildFailure()
        {
            failure = UiKit.Fill(UiKit.NewRect("Failure", Rect));
            var dim = failure.gameObject.AddComponent<Image>();
            dim.color = Theme.WithAlpha(Theme.Navy, 0.82f);

            var card = UiKit.Panel(failure, "Card", Theme.Card, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 900f));

            var face = UiKit.Disc(card.rectTransform, "Face", Theme.Orange, 150f);
            UiKit.Place(face.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), face.rectTransform.sizeDelta);
            var mark = UiKit.Label(face.rectTransform, "!", 110f, Theme.Ink, TextAlignmentOptions.Center, true, "Mark");
            UiKit.Fill(mark.rectTransform);

            var title = UiKit.Label(card.rectTransform, AppText.ErrorTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(1300f, 110f));
            failureText = UiKit.Label(card.rectTransform, "", Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, false, "Text");
            UiKit.Place(failureText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(1300f, 160f));

            failureRetry = UiKit.Button(card.rectTransform, AppText.Retry, UiKit.ButtonStyle.Primary, new Vector2(560f, Theme.ButtonHeight),
                () => Context.Flow.BeginCapture(false), "Retry", Theme.H2);
            UiKit.Place(failureRetry.Rect, new Vector2(0.72f, 0f), new Vector2(0f, 60f), failureRetry.Rect.sizeDelta);
            var home = UiKit.Button(card.rectTransform, AppText.Back, UiKit.ButtonStyle.Soft, new Vector2(460f, Theme.ButtonHeight),
                () => Context.Flow.LeaveToHome(), "Home", Theme.H2);
            UiKit.Place(home.Rect, new Vector2(0.27f, 0f), new Vector2(0f, 60f), home.Rect.sizeDelta);
            failureSettings = UiKit.Button(card.rectTransform, AppText.ErrorCameraOpenSettings, UiKit.ButtonStyle.Secondary,
                new Vector2(560f, Theme.SmallButtonHeight), () => Application.OpenURL("app-settings:"), "Settings", Theme.Body);
            UiKit.Place(failureSettings.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 250f), failureSettings.Rect.sizeDelta);
            failure.gameObject.SetActive(false);
        }

        public override void OnEnter(object arg)
        {
            hadTop = false;
            lastStatus = StageContext.ScanStatus;
            flashTime = 1f;
            flash.color = new Color(1f, 1f, 1f, 0f);
            warning.text = "";
            if (arg is FailureKind kind)
            {
                ShowFailure(kind);
            }
            else
            {
                failed = false;
                failure.gameObject.SetActive(false);
            }
        }

        void ShowFailure(FailureKind kind)
        {
            failed = true;
            failureText.text = FlowRules.FailureText(kind);
            failureSettings.gameObject.SetActive(kind == FailureKind.CameraDenied);
            failureRetry.gameObject.SetActive(true);
            failure.gameObject.SetActive(true);
            failure.SetAsLastSibling();
        }

        public override void OnBack()
        {
            if (failed || !hadTop)
            {
                Context.Flow.LeaveToHome();
                return;
            }
            Context.Overlays.Confirm(AppText.CaptureLeaveConfirm, () => Context.Flow.LeaveToHome(), AppText.Back, AppText.No);
        }

        void OnShutter()
        {
            if (!shutter.interactable)
            {
                return;
            }
            UiSound.Play(UiSound.Cue.Shutter);
            Context.Flow.TakePhoto();
        }

        void Update()
        {
            if (failed)
            {
                return;
            }
            if (photo == null)
            {
                photo = FindFirstObjectByType<PhotoCapture>();
            }
            var hasTop = photo != null && photo.TopJpeg != null;
            var hasSide = photo != null && photo.SideJpeg != null;
            var status = StageContext.ScanStatus;
            var texture = ScanContext.CameraPreview;
            preview.Show(texture);

            var step = FlowRules.CaptureStepFor(hasTop, hasSide, status);
            preparing.gameObject.SetActive(texture == null && step != CaptureStep.Sending);
            var ready = texture != null && (step == CaptureStep.Top || step == CaptureStep.Side);
            shutter.interactable = ready;
            shutterGroup.alpha = ready ? 1f : 0.35f;

            chip1.SetState(hasTop ? 2 : 1);
            chip2.SetState(hasTop ? 1 : 0);
            hint.text = hasTop ? AppText.CaptureHint2 : AppText.CaptureHint1;

            // 使えない写真だったとき（暗い、ぶれた）
            if (status == ScanStatus.Limited)
            {
                warning.text = StageContext.ScanReason == ScanReason.Dark ? AppText.CaptureLimitedDark : AppText.CaptureLimitedBlur;
                if (lastStatus != ScanStatus.Limited)
                {
                    UiSound.Play(UiSound.Cue.Error);
                }
            }
            else if (hasTop && !hadTop)
            {
                warning.text = AppText.CaptureTookTop;
                warning.color = Theme.Green;
            }
            else if (status != ScanStatus.Limited && lastStatus == ScanStatus.Limited)
            {
                warning.text = "";
            }
            if (status == ScanStatus.Limited)
            {
                warning.color = Theme.Orange;
            }

            if (hasTop && !hadTop)
            {
                flashTime = 0f;
            }
            hadTop = hasTop;
            lastStatus = status;

            if (flashTime < 1f)
            {
                flashTime += Time.unscaledDeltaTime / 0.35f;
                flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - flashTime) * 0.8f);
            }

            if (hasSide && status != ScanStatus.Lost)
            {
                Context.Flow.PhotosSent();
            }
        }
    }
}
