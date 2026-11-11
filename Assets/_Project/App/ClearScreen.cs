using DeskStage.Shared;
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

            // タイムは左、記録は右に並べる（2026-11-11）
            var card = UiKit.Panel(safe, "TimeCard", Color.white, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-330f, 30f), new Vector2(880f, 220f));
            time = UiKit.Label(card.rectTransform, "", Theme.Hero * 0.8f, Theme.Ink, TextAlignmentOptions.Center, true, "Time");
            UiKit.Fill(time.rectTransform);
            BuildRecords(safe);

            // そのときのしばり（Docs/specs/course-rules。記録は、しばりで分けない）
            rulesLine = UiKit.Label(safe, "", Theme.H2, Theme.Ink, TextAlignmentOptions.Center, true, "Rules");
            UiKit.Place(rulesLine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1600f, 90f));

            var badge = UiKit.Panel(safe, "Record", Theme.Red, 60f);
            record = badge.rectTransform;
            UiKit.Place(record, new Vector2(0.5f, 0.5f), new Vector2(60f, 170f), new Vector2(420f, 110f));
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

        TMP_Text rulesLine;

        // ---- 記録のカード（StageRecords。2026-11-11） ----
        // 新記録：前の記録と、どれだけ速くなったか。そうでないとき：これまでの上位 3 つ（今回が入っていれば、その行に「いま」）
        TMP_Text recordsTitle;
        TMP_Text previousTime;
        TMP_Text faster;
        readonly TMP_Text[] rows = new TMP_Text[StageRecords.Keep];
        readonly Image[] rowMarks = new Image[StageRecords.Keep];

        void BuildRecords(RectTransform safe)
        {
            var card = UiKit.Panel(safe, "RecordsCard", Color.white, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(450f, 30f), new Vector2(580f, 290f));
            var area = card.rectTransform;

            recordsTitle = UiKit.Label(area, "", Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, true, "RecordsTitle");
            UiKit.Area(recordsTitle.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 0.97f), 16f);

            // 新記録のとき：前の記録（大きく）と、どれだけ速くなったか（1 行。入りきらなければ文字を小さく）
            previousTime = UiKit.Label(area, "", Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "PreviousTime");
            UiKit.Area(previousTime.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.8f), 16f);
            faster = UiKit.FitLabel(area, "", Theme.H2, Theme.Red, TextAlignmentOptions.Center, true, "Faster");
            UiKit.Area(faster.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.5f), 10f);

            // 新記録でないとき：上位 3 つの行
            for (var i = 0; i < StageRecords.Keep; i++)
            {
                var top = 0.76f - i * 0.24f;
                var mark = UiKit.Panel(area, "RowMark" + i, Theme.WithAlpha(Theme.Orange, 0.22f), 24f);
                UiKit.Area(mark.rectTransform, new Vector2(0.06f, top - 0.22f), new Vector2(0.94f, top), 0f);
                mark.raycastTarget = false;
                var nowTag = UiKit.Label(mark.rectTransform, AppText.ClearNow, Theme.Body, Theme.Orange, TextAlignmentOptions.Right, true, "Now");
                UiKit.Area(nowTag.rectTransform, new Vector2(0.7f, 0f), new Vector2(0.97f, 1f), 0f);
                rowMarks[i] = mark;
                rows[i] = UiKit.Label(area, "", Theme.H2, Theme.Ink, TextAlignmentOptions.Left, true, "Row" + i);
                UiKit.Area(rows[i].rectTransform, new Vector2(0.14f, top - 0.22f), new Vector2(0.8f, top), 0f);
            }
        }

        void ShowRecords(ClearInfo info)
        {
            var best = info.IsBest;
            previousTime.gameObject.SetActive(best);
            faster.gameObject.SetActive(best);
            if (best)
            {
                if (info.PreviousBest.HasValue)
                {
                    recordsTitle.text = AppText.ClearPreviousBest;
                    previousTime.text = FlowRules.FormatTime(info.PreviousBest.Value);
                    faster.text = string.Format(AppText.ClearFaster, FlowRules.FormatGain(info.PreviousBest.Value, info.Seconds));
                    UiKit.Area(faster.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.5f), 10f);
                }
                else
                {
                    // はじめてのクリア：前の記録は無いので、カードの真ん中に、ひとことだけ
                    recordsTitle.text = "";
                    previousTime.text = "";
                    faster.text = AppText.ClearFirst;
                    UiKit.Area(faster.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), 24f);
                }
            }
            else
            {
                recordsTitle.text = AppText.ClearRanking;
            }
            var top = info.Top ?? new float[0];
            for (var i = 0; i < StageRecords.Keep; i++)
            {
                var show = !best && i < top.Length;
                rows[i].gameObject.SetActive(show);
                rowMarks[i].gameObject.SetActive(show && i == info.Rank);
                if (show) rows[i].text = string.Format(AppText.ClearRankRow, i + 1, FlowRules.FormatTime(top[i]));
            }
        }

        public override void OnEnter(object arg)
        {
            var info = arg as ClearInfo ?? new ClearInfo();
            time.text = string.Format(AppText.ClearTime, FlowRules.FormatTime(info.Seconds));
            record.gameObject.SetActive(info.IsBest);
            ShowRecords(info);
            var summary = CourseRuleSystem.Summary(StageContext.CurrentStage != null ? StageContext.CurrentStage.rules : null);
            rulesLine.text = info.MissionDone ? AppText.MissionClear
                : summary.Length > 0 ? string.Format(AppText.RulesClear, summary) : "";
            rulesLine.color = info.MissionDone ? Theme.Red : Theme.Ink;
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
