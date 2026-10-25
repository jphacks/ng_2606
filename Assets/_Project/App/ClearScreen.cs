using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// クリア。「ゴール！」とタイム（記録を更新したら「しんきろく！」）。紙吹雪が降る。
    /// 「もういちど あそぶ」が、いちばん大きい。ほかに、「いちらんへ」、撮ったばかりのステージなら「ほぞん」と「コースを なおす」。
    /// </summary>
    public class ClearScreen : AppScreen
    {
        TMP_Text time;
        RectTransform record;
        AppButton save;
        AppButton fix;
        Transform confetti;

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Sky, Theme.SkyLight);
            confetti = UiKit.Fill(UiKit.NewRect("Confetti", Rect));
            for (var i = 0; i < 28; i++)
            {
                var piece = UiKit.Panel(confetti, "P" + i, Theme.BlockColors[i % Theme.BlockColors.Length], 6f);
                var size = 26f + (i * 7) % 24;
                UiKit.Place(piece.rectTransform, new Vector2((i * 37 % 100) / 100f, 1f), new Vector2(0f, -(i * 53 % 1170)), new Vector2(size, size * 0.6f));
                piece.raycastTarget = false;
                var rain = piece.gameObject.AddComponent<ConfettiPiece>();
                rain.Speed = 140f + (i * 29) % 160;
                rain.Spin = (i % 2 == 0 ? 1f : -1f) * (60f + (i * 13) % 120);
            }

            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            var shadow = UiKit.Label(safe, AppText.ClearTitle, Theme.Hero, Color.white, TextAlignmentOptions.Center, true, "TitleShadow");
            UiKit.Place(shadow.rectTransform, new Vector2(0.5f, 1f), new Vector2(6f, -100f), new Vector2(1400f, 260f));
            var title = UiKit.Label(safe, AppText.ClearTitle, Theme.Hero, Theme.Orange, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(1400f, 260f));

            var card = UiKit.Panel(safe, "TimeCard", Color.white, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1000f, 220f));
            time = UiKit.Label(card.rectTransform, "", Theme.Hero * 0.8f, Theme.Ink, TextAlignmentOptions.Center, true, "Time");
            UiKit.Fill(time.rectTransform);

            var badge = UiKit.Panel(safe, "Record", Theme.Red, 60f);
            record = badge.rectTransform;
            UiKit.Place(record, new Vector2(0.5f, 0.5f), new Vector2(420f, 170f), new Vector2(420f, 110f));
            record.localRotation = Quaternion.Euler(0f, 0f, 8f);
            var recordLabel = UiKit.FitLabel(record, AppText.ClearNewRecord, Theme.H2, Color.white, TextAlignmentOptions.Center, true, "L");
            UiKit.Fill(recordLabel.rectTransform);
            record.gameObject.AddComponent<Bobbing>().Amplitude = 8f;

            var again = UiKit.Button(safe, AppText.ClearRetry, UiKit.ButtonStyle.Primary, new Vector2(820f, Theme.ButtonHeight),
                () => Context.Flow.PlayAgain(), "Again", Theme.H1);
            UiKit.Place(again.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 240f), again.Rect.sizeDelta);

            var list = UiKit.Button(safe, AppText.ClearList, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                () => Context.Flow.BackToList(), "List", Theme.Body);
            UiKit.Place(list.Rect, new Vector2(0.5f, 0f), new Vector2(-300f, 60f), list.Rect.sizeDelta);
            save = UiKit.Button(safe, AppText.CheckSave, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                () => Context.Flow.SaveStage(), "Save", Theme.Body);
            UiKit.Place(save.Rect, new Vector2(0.5f, 0f), new Vector2(300f, 60f), save.Rect.sizeDelta);
            fix = UiKit.Button(safe, AppText.ClearFix, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                () => Context.Router.Back(), "Fix", Theme.Body);
            UiKit.Place(fix.Rect, new Vector2(1f, 0f), new Vector2(-Theme.Margin, 60f), fix.Rect.sizeDelta);
        }

        public override void OnEnter(object arg)
        {
            var info = arg as ClearInfo ?? new ClearInfo();
            time.text = string.Format(AppText.ClearTime, FlowRules.FormatTime(info.Seconds));
            record.gameObject.SetActive(info.IsBest);
            UiSound.Play(UiSound.Cue.Success);
            Refresh();
        }

        void Update() => Refresh();

        // 撮ったばかりのステージ（まだ保存していない、または、コースを直せる）のときだけ、「ほぞん」と「なおす」を出す
        void Refresh()
        {
            var isNew = Context.Flow.HasNewStage && !Context.Flow.PlayingSaved;
            save.gameObject.SetActive(isNew);
            fix.gameObject.SetActive(isNew);
            if (isNew)
            {
                var saved = Context.Flow.NewStageSaved;
                save.SetText(saved ? AppText.CheckSaved : AppText.CheckSave);
                save.Interactable = !saved;
            }
        }

        public override void OnBack() => Context.Flow.BackToList();
    }

    /// <summary>ゆっくり落ちて、まわる紙吹雪（1 枚）。下まで行ったら、上に戻る。</summary>
    public class ConfettiPiece : MonoBehaviour
    {
        public float Speed = 200f;
        public float Spin = 90f;

        void Update()
        {
            var rect = (RectTransform)transform;
            var p = rect.anchoredPosition;
            p.y -= Speed * Time.unscaledDeltaTime;
            if (p.y < -1250f)
            {
                p.y = 80f;
            }
            rect.anchoredPosition = p;
            rect.Rotate(0f, 0f, Spin * Time.unscaledDeltaTime);
        }
    }
}
