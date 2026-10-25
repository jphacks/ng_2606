using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// どの画面にも重なる部品：確認の画面（Confirm）、ひとこと表示（Toast）、読み込み中の表示（Busy）。
    /// 画面いっぱいの入れ物（Full）の、いちばん手前に置く。画面のコードは、ここを呼ぶだけで、同じ見た目で出せる。
    /// </summary>
    public class AppOverlays : MonoBehaviour
    {
        public enum ToastKind
        {
            Info,
            Good,
            Bad,
        }

        RectTransform confirmRoot;
        TMP_Text confirmText;
        AppButton confirmYes;
        AppButton confirmNo;
        Action confirmAction;

        RectTransform toastRoot;
        Image toastBackground;
        TMP_Text toastText;
        Coroutine toastRoutine;

        RectTransform busyRoot;
        TMP_Text busyText;

        public bool IsConfirmOpen => confirmRoot != null && confirmRoot.gameObject.activeSelf;

        public bool IsBusy => busyRoot != null && busyRoot.gameObject.activeSelf;

        /// <summary>画面いっぱいの入れ物に、部品を作る。</summary>
        public void Build(RectTransform full)
        {
            BuildBusy(full);
            BuildConfirm(full);
            BuildToast(full);
        }

        // ---- 確認 ----

        void BuildConfirm(RectTransform full)
        {
            confirmRoot = UiKit.Fill(UiKit.NewRect("Confirm", full));
            var dim = confirmRoot.gameObject.AddComponent<Image>();
            dim.color = Theme.WithAlpha(Theme.Navy, 0.6f); // 後ろを押せないように、全面でタップを受ける

            var card = UiKit.Panel(confirmRoot, "Card", Theme.Card, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 700f));

            confirmText = UiKit.Label(card.rectTransform, "", Theme.H2, Theme.Ink, TextAlignmentOptions.Center, true);
            UiKit.Area(confirmText.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 1f), 56f);

            confirmNo = UiKit.Button(card.rectTransform, AppText.No, UiKit.ButtonStyle.Soft, new Vector2(560f, Theme.ButtonHeight), Close, "No");
            UiKit.Place(confirmNo.Rect, new Vector2(0.25f, 0f), new Vector2(0f, 70f), confirmNo.Rect.sizeDelta);
            confirmNo.Rect.pivot = new Vector2(0.5f, 0f);
            confirmYes = UiKit.Button(card.rectTransform, AppText.Yes, UiKit.ButtonStyle.Primary, new Vector2(560f, Theme.ButtonHeight), Accept, "Yes");
            UiKit.Place(confirmYes.Rect, new Vector2(0.75f, 0f), new Vector2(0f, 70f), confirmYes.Rect.sizeDelta);
            confirmYes.Rect.pivot = new Vector2(0.5f, 0f);
            confirmRoot.gameObject.SetActive(false);
        }

        /// <summary>「いい？」と聞く。「うん」を押したら onYes を呼ぶ。</summary>
        public void Confirm(string message, Action onYes, string yes = null, string no = null, bool danger = false)
        {
            confirmText.text = message;
            confirmYes.SetText(yes ?? AppText.Yes);
            confirmYes.SetStyle(danger ? UiKit.ButtonStyle.Danger : UiKit.ButtonStyle.Primary);
            confirmNo.SetText(no ?? AppText.No);
            confirmAction = onYes;
            confirmRoot.SetAsLastSibling();
            confirmRoot.gameObject.SetActive(true);
        }

        void Accept()
        {
            var action = confirmAction;
            Close();
            action?.Invoke();
        }

        public void Close()
        {
            confirmAction = null;
            if (confirmRoot != null) confirmRoot.gameObject.SetActive(false);
        }

        // ---- ひとこと ----

        void BuildToast(RectTransform full)
        {
            toastRoot = UiKit.NewRect("Toast", full);
            UiKit.Place(toastRoot, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1500f, 150f));
            toastBackground = UiKit.Panel(toastRoot, "Back", Theme.Navy, Theme.ButtonRadius, shadow: true);
            UiKit.Fill(toastBackground.rectTransform);
            toastBackground.raycastTarget = false;
            toastText = UiKit.FitLabel(toastRoot, "", Theme.Body, Color.white, TextAlignmentOptions.Center, true);
            UiKit.Area(toastText.rectTransform, Vector2.zero, Vector2.one, 24f);
            toastRoot.gameObject.SetActive(false);
        }

        /// <summary>画面の上に、ひとこと出す（数秒で消える）。</summary>
        public void Toast(string message, ToastKind kind = ToastKind.Info, float seconds = 2.6f)
        {
            if (toastRoot == null)
            {
                return;
            }
            switch (kind)
            {
                case ToastKind.Good:
                    toastBackground.color = Theme.Green;
                    UiSound.Play(UiSound.Cue.Success);
                    break;
                case ToastKind.Bad:
                    toastBackground.color = Theme.Red;
                    UiSound.Play(UiSound.Cue.Error);
                    break;
                default:
                    toastBackground.color = Theme.WithAlpha(Theme.Navy, 0.92f);
                    break;
            }
            toastText.text = message;
            toastRoot.SetAsLastSibling();
            toastRoot.gameObject.SetActive(true);
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            if (isActiveAndEnabled) toastRoutine = StartCoroutine(HideToastLater(seconds));
        }

        IEnumerator HideToastLater(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            toastRoot.gameObject.SetActive(false);
        }

        // ---- 読み込み中 ----

        void BuildBusy(RectTransform full)
        {
            busyRoot = UiKit.Fill(UiKit.NewRect("Busy", full));
            busyRoot.gameObject.AddComponent<Image>().color = Theme.WithAlpha(Theme.Navy, 0.55f);
            var card = UiKit.Panel(busyRoot, "Card", Theme.Card, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 480f));
            var spinner = UiKit.Spinner(card.rectTransform, "Spinner", Theme.Orange, 190f);
            UiKit.Place((RectTransform)spinner.transform, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(190f, 190f));
            busyText = UiKit.Label(card.rectTransform, "", Theme.H2, Theme.Ink, TextAlignmentOptions.Center, true);
            UiKit.Area(busyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.38f), 24f);
            busyRoot.gameObject.SetActive(false);
        }

        /// <summary>読み込み中の表示を出す（後ろは押せない）。</summary>
        public void Busy(string message)
        {
            busyText.text = message;
            busyRoot.SetAsLastSibling();
            busyRoot.gameObject.SetActive(true);
        }

        public void HideBusy()
        {
            if (busyRoot != null) busyRoot.gameObject.SetActive(false);
        }
    }
}
