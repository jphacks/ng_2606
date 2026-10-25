using DeskStage.AR;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// 写真を送って、ステージができるのを待っている間の画面。動いていることが、ひと目で分かるように：
    /// 回り続ける輪、はねる積み木、左右に動くバー、増えていく秒数、4 秒ごとに替わるひとこと、撮った 1 枚目の写真。
    /// 答えが届く・失敗する・やめる、のどれかで、AppFlow が別の画面へ進める。
    /// </summary>
    public class GeneratingScreen : AppScreen
    {
        const float TipSeconds = 4f;
        const float LongWaitSeconds = 45f;
        const float BarWidth = 900f;
        const float BarHighlight = 280f;

        TMP_Text seconds;
        TMP_Text tip;
        RectTransform highlight;
        RawImage photoImage;
        AspectRatioFitter photoFitter;
        GameObject photoPlaceholder;
        Texture2D photoTexture;

        float since;
        int tipIndex;
        float nextTip;

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Sky, Theme.SkyLight);
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            // 左：撮った写真（少し傾けた、白ふちの写真）
            var card = UiKit.Panel(safe, "Photo", Color.white, 30f, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.27f, 0.5f), new Vector2(0f, 10f), new Vector2(780f, 620f));
            card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            var inner = UiKit.NewRect("Inner", card.rectTransform);
            UiKit.Area(inner, Vector2.zero, Vector2.one, 24f);
            inner.offsetMin = new Vector2(24f, 90f);
            var frame = UiKit.Panel(inner, "Frame", Theme.SkyLight, 14f);
            UiKit.Fill(frame.rectTransform);
            var imageRect = UiKit.Fill(UiKit.NewRect("Image", frame.rectTransform));
            photoImage = imageRect.gameObject.AddComponent<RawImage>();
            photoImage.raycastTarget = false;
            photoFitter = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            photoFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            photoFitter.aspectRatio = 4f / 3f;
            photoPlaceholder = UiKit.NewRect("Placeholder", frame.rectTransform).gameObject;
            UiKit.Fill((RectTransform)photoPlaceholder.transform);
            var caption = UiKit.Label(card.rectTransform, AppText.GeneratingPhotoCaption, Theme.Small, Theme.InkSoft, TextAlignmentOptions.Center, true, "Caption");
            UiKit.Area(caption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), 0f);
            caption.rectTransform.sizeDelta = new Vector2(0f, 80f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, 45f);

            // 右：はねる積み木、題、秒数、バー、ひとこと
            var blocks = UiKit.NewRect("Blocks", safe);
            UiKit.Place(blocks, new Vector2(0.72f, 0.5f), new Vector2(0f, 330f), new Vector2(400f, 140f));
            for (var i = 0; i < 3; i++)
            {
                var size = 90f - i * 8f;
                var block = UiKit.Panel(blocks, "Block" + i, Theme.BlockColors[(i * 2) % Theme.BlockColors.Length], size * 0.22f, shadow: true);
                UiKit.Place(block.rectTransform, new Vector2(0.5f, 0f), new Vector2((i - 1) * 130f, 10f), new Vector2(size, size));
                var bob = block.gameObject.AddComponent<Bobbing>();
                bob.Amplitude = 36f;
                bob.Speed = 2.4f;
                bob.Phase = i * 0.9f;
            }

            var title = UiKit.FitLabel(safe, AppText.Generating, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.72f, 0.5f), new Vector2(0f, 190f), new Vector2(1100f, 130f));

            var spinner = UiKit.Spinner(safe, "Spinner", Theme.Orange, 200f);
            UiKit.Place(spinner.GetComponent<RectTransform>(), new Vector2(0.72f, 0.5f), new Vector2(-160f, 20f), new Vector2(200f, 200f));
            seconds = UiKit.FitLabel(safe, "", 96f, Theme.Ink, TextAlignmentOptions.Left, true, "Seconds");
            seconds.textWrappingMode = TextWrappingModes.NoWrap; // 「びょう」の「う」だけが、次の行に落ちないように
            UiKit.Place(seconds.rectTransform, new Vector2(0.72f, 0.5f), new Vector2(-30f, 20f), new Vector2(640f, 200f));
            seconds.rectTransform.pivot = new Vector2(0f, 0.5f);
            seconds.rectTransform.anchoredPosition = new Vector2(-30f, 20f);

            var track = UiKit.Panel(safe, "Track", Theme.WithAlpha(Color.white, 0.8f), 14f);
            UiKit.Place(track.rectTransform, new Vector2(0.72f, 0.5f), new Vector2(0f, -130f), new Vector2(BarWidth, 28f));
            var mask = track.gameObject.AddComponent<RectMask2D>();
            highlight = UiKit.Panel(track.rectTransform, "Highlight", Theme.Orange, 14f).rectTransform;
            highlight.anchorMin = new Vector2(0f, 0f);
            highlight.anchorMax = new Vector2(0f, 1f);
            highlight.pivot = new Vector2(0f, 0.5f);
            highlight.sizeDelta = new Vector2(BarHighlight, 0f);
            _ = mask;

            tip = UiKit.Label(safe, AppText.GeneratingTips[0], Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, false, "Tip");
            tip.textWrappingMode = TextWrappingModes.NoWrap; // 改行は、文の中の \n だけ（1 文字だけ次の行に落ちないように）
            UiKit.Place(tip.rectTransform, new Vector2(0.72f, 0.5f), new Vector2(0f, -230f), new Vector2(1100f, 140f));

            var cancel = UiKit.Button(safe, AppText.GeneratingCancel, UiKit.ButtonStyle.Soft, new Vector2(460f, Theme.SmallButtonHeight),
                () => OnBack(), "Cancel", Theme.Body);
            UiKit.Place(cancel.Rect, new Vector2(0.72f, 0f), new Vector2(0f, 60f), cancel.Rect.sizeDelta);
        }

        public override void OnEnter(object arg)
        {
            since = Time.realtimeSinceStartup;
            tipIndex = 0;
            nextTip = since + TipSeconds;
            tip.text = AppText.GeneratingTips[0];
            ShowPhoto();
        }

        public override void OnExit()
        {
            if (photoTexture != null)
            {
                UiKit.Kill(photoTexture);
                photoTexture = null;
            }
        }

        void ShowPhoto()
        {
            var capture = FindFirstObjectByType<PhotoCapture>();
            var jpeg = capture != null ? capture.TopJpeg : null;
            if (photoTexture != null)
            {
                UiKit.Kill(photoTexture);
                photoTexture = null;
            }
            if (jpeg != null)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (texture.LoadImage(jpeg))
                {
                    photoTexture = texture;
                    photoImage.texture = texture;
                    photoFitter.aspectRatio = (float)texture.width / texture.height;
                }
                else
                {
                    UiKit.Kill(texture);
                }
            }
            photoImage.enabled = photoTexture != null;
            photoPlaceholder.SetActive(photoTexture == null);
        }

        // やめる：確認してから、撮影からやり直す
        public override void OnBack()
        {
            Context.Overlays.Confirm(AppText.GeneratingCancelConfirm, () => Context.Flow.CancelGeneration(), AppText.GeneratingCancel, AppText.GeneratingKeep);
        }

        void Update()
        {
            var now = Time.realtimeSinceStartup;
            var elapsed = Mathf.Max(0f, now - since);
            seconds.text = string.Format(AppText.GeneratingSeconds, Mathf.FloorToInt(elapsed));

            if (now >= nextTip)
            {
                tipIndex = (tipIndex + 1) % AppText.GeneratingTips.Length;
                nextTip = now + TipSeconds;
                tip.text = AppText.GeneratingTips[tipIndex];
            }
            if (elapsed > LongWaitSeconds)
            {
                tip.text = AppText.GeneratingLong;
            }

            // バーの光る部分が、左右に行き来する
            var span = BarWidth - BarHighlight;
            var k = Mathf.PingPong(now * 0.7f, 1f);
            var eased = k * k * (3f - 2f * k);
            highlight.anchoredPosition = new Vector2(eased * span, 0f);
        }
    }
}
