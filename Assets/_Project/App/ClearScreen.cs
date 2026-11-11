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

            // タイムと記録を 1 枚の大きなカードに（2026-11-11）：上の段にタイム、線で区切って、下の段に記録。
            // しばりやお題があるときは、いちばん下に、黄色の帯の段を足す（大きさと段の分け方は LayoutCard）
            var card = UiKit.Panel(safe, "TimeCard", Color.white, Theme.CardRadius, shadow: true);
            cardRect = card.rectTransform;
            time = UiKit.Label(cardRect, "", Theme.Hero * 0.8f, Theme.Ink, TextAlignmentOptions.Center, true, "Time");
            divider = UiKit.Panel(cardRect, "Line", Theme.WithAlpha(Theme.InkSoft, 0.18f), 2f).rectTransform;
            recordsRect = UiKit.NewRect("Records", cardRect);
            BuildRecords(recordsRect);

            // そのときのしばり（Docs/specs/course-rules。記録は、しばりで分けない）。長いときは、1 行に収まるよう文字を小さくする
            var band = UiKit.Panel(cardRect, "RulesBand", Theme.WithAlpha(Theme.Orange, 0.14f), 30f);
            band.raycastTarget = false;
            rulesBand = band.rectTransform;
            rulesLine = UiKit.FitLabel(rulesBand, "", Theme.H2, Theme.Ink, TextAlignmentOptions.Center, true, "Rules");
            UiKit.Area(rulesLine.rectTransform, Vector2.zero, Vector2.one, 18f);
            LayoutCard(false);

            var badge = UiKit.Panel(safe, "Record", Theme.Red, 60f);
            record = badge.rectTransform;
            // カードの右上の角の外（タイムの数字に重ならない）
            UiKit.Place(record, new Vector2(0.5f, 0.5f), new Vector2(540f, 262f), new Vector2(420f, 110f));
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
        RectTransform cardRect;
        RectTransform divider;
        RectTransform recordsRect;
        RectTransform rulesBand;

        // カードの大きさと段の分け方。しばり・お題の帯があるときは、カードを下へ伸ばし、いちばん下に帯の段を足す
        // （前は、カードの外に 1 行で出していて、長いと折り返して、カードやボタンに重なっていた）
        void LayoutCard(bool withRules)
        {
            var height = withRules ? 430f : 350f;
            UiKit.Place(cardRect, new Vector2(0.5f, 0.5f), new Vector2(0f, withRules ? 45f : 65f), new Vector2(1160f, height));
            // 段の境目（下から、カードの高さに対する割合）
            var rulesTop = withRules ? 90f / height : 0f;
            var timeBottom = rulesTop + (1f - rulesTop) * 0.45f;
            UiKit.Area(time.rectTransform, new Vector2(0f, timeBottom), new Vector2(1f, 1f), 10f);
            UiKit.Area(divider, new Vector2(0.05f, timeBottom), new Vector2(0.95f, timeBottom + 2f / height), 0f);
            UiKit.Area(recordsRect, new Vector2(0.03f, rulesTop + 12f / height), new Vector2(0.97f, timeBottom - 6f / height), 0f);
            rulesBand.gameObject.SetActive(withRules);
            UiKit.Area(rulesBand, new Vector2(0.03f, 14f / height), new Vector2(0.97f, rulesTop), 0f);
        }

        // 今回、しばりかお題の帯を出すか（文の中身は OnEnter で入れる）
        static bool HasRulesLine(ClearInfo info)
        {
            return info.MissionDone || CourseRuleSystem.Summary(StageContext.CurrentStage != null ? StageContext.CurrentStage.rules : null).Length > 0;
        }

        // ---- 記録の列（StageRecords。2026-11-11） ----
        // 横 1 列に出す。新記録：「まえの きろく 0:25.5」と、緑の札「2.1びょう はやく なった！」（はじめてなら、札だけ「はじめての クリア！」）。
        // 札は、赤い「しんきろく！」と見分けやすいよう、緑にする。
        // そうでないとき：「これまでの きろく」と、金・銀・銅のメダルに添えたタイム 3 つ（今回が入っていれば、そのメダルを囲み、「いま」）
        static readonly Color[] MedalColors =
        {
            new Color(1f, 0.78f, 0.15f),  // 金
            new Color(0.72f, 0.75f, 0.8f), // 銀
            new Color(0.82f, 0.52f, 0.3f), // 銅
        };

        TMP_Text previousLine;
        RectTransform gainPill;
        TMP_Text gainText;
        TMP_Text rankTitle;
        readonly GameObject[] chips = new GameObject[StageRecords.Keep];
        readonly TMP_Text[] chipTimes = new TMP_Text[StageRecords.Keep];
        readonly GameObject[] chipNow = new GameObject[StageRecords.Keep];

        void BuildRecords(RectTransform row)
        {
            // 新記録のとき
            previousLine = UiKit.FitLabel(row, "", Theme.H2, Theme.Ink, TextAlignmentOptions.Right, true, "Previous");
            UiKit.Area(previousLine.rectTransform, new Vector2(0f, 0f), new Vector2(0.46f, 1f), 10f);
            var pill = UiKit.Panel(row, "Gain", Theme.Green, 50f);
            gainPill = pill.rectTransform;
            pill.raycastTarget = false;
            gainText = UiKit.FitLabel(gainPill, "", Theme.H2, Color.white, TextAlignmentOptions.Center, true, "GainText");
            UiKit.Area(gainText.rectTransform, Vector2.zero, Vector2.one, 14f);

            // 新記録でないとき
            rankTitle = UiKit.FitLabel(row, AppText.ClearRanking, Theme.Body, Theme.InkSoft, TextAlignmentOptions.Center, true, "RankTitle");
            UiKit.Area(rankTitle.rectTransform, new Vector2(0f, 0f), new Vector2(0.22f, 1f), 8f);
            for (var i = 0; i < StageRecords.Keep; i++)
            {
                var chip = UiKit.Panel(row, "Medal" + i, Theme.WithAlpha(Theme.Orange, 0.2f), 50f);
                chip.raycastTarget = false;
                var x = 0.24f + i * 0.255f;
                UiKit.Area(chip.rectTransform, new Vector2(x, 0.04f), new Vector2(x + 0.24f, 0.96f), 0f);
                // 囲み（今回の記録だけ見せる）。チップの下地は透明にし、囲みのときだけ色を付ける
                chipNow[i] = chip.gameObject;
                var holder = UiKit.NewRect("Body", row);
                UiKit.Area(holder, new Vector2(x, 0.04f), new Vector2(x + 0.24f, 0.96f), 0f);
                var medal = UiKit.Disc(holder, "Disc", MedalColors[i], 74f);
                UiKit.Place(medal.rectTransform, new Vector2(0f, 0.5f), new Vector2(52f, 0f), medal.rectTransform.sizeDelta);
                medal.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                medal.raycastTarget = false;
                var number = UiKit.Label(medal.rectTransform, (i + 1).ToString(), Theme.H2, Color.white, TextAlignmentOptions.Center, true, "N");
                UiKit.Fill(number.rectTransform);
                chipTimes[i] = UiKit.FitLabel(holder, "", Theme.H2, Theme.Ink, TextAlignmentOptions.Left, true, "Time");
                UiKit.Area(chipTimes[i].rectTransform, new Vector2(0.4f, 0f), new Vector2(1f, 1f), 6f);
                // 「いま」の札：囲みの上の端に、半分はみ出して付ける
                var nowTag = UiKit.Panel(chip.rectTransform, "Now", Theme.Orange, 22f);
                nowTag.raycastTarget = false;
                UiKit.Place(nowTag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(96f, 44f));
                nowTag.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                nowTag.rectTransform.anchoredPosition = Vector2.zero;
                var nowText = UiKit.Label(nowTag.rectTransform, AppText.ClearNow, Theme.Body * 0.75f, Color.white, TextAlignmentOptions.Center, true, "T");
                UiKit.Fill(nowText.rectTransform);
                chips[i] = holder.gameObject;
            }
        }

        void ShowRecords(ClearInfo info)
        {
            var best = info.IsBest;
            previousLine.gameObject.SetActive(best && info.PreviousBest.HasValue);
            gainPill.gameObject.SetActive(best);
            if (best)
            {
                if (info.PreviousBest.HasValue)
                {
                    previousLine.text = AppText.ClearPreviousBest + "　" + FlowRules.FormatTime(info.PreviousBest.Value);
                    gainText.text = string.Format(AppText.ClearFaster, FlowRules.FormatGain(info.PreviousBest.Value, info.Seconds));
                    UiKit.Area(gainPill, new Vector2(0.5f, 0.08f), new Vector2(0.98f, 0.92f), 0f);
                }
                else
                {
                    // はじめてのクリア：前の記録は無いので、札だけを真ん中に
                    gainText.text = AppText.ClearFirst;
                    UiKit.Area(gainPill, new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.92f), 0f);
                }
            }
            rankTitle.gameObject.SetActive(!best);
            var top = info.Top ?? new float[0];
            for (var i = 0; i < StageRecords.Keep; i++)
            {
                var show = !best && i < top.Length;
                chips[i].SetActive(show);
                chipNow[i].SetActive(show && i == info.Rank);
                if (show) chipTimes[i].text = FlowRules.FormatTime(top[i]);
            }
        }

        public override void OnEnter(object arg)
        {
            var info = arg as ClearInfo ?? new ClearInfo();
            time.text = string.Format(AppText.ClearTime, FlowRules.FormatTime(info.Seconds));
            record.gameObject.SetActive(info.IsBest);
            ShowRecords(info);
            LayoutCard(HasRulesLine(info));
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
