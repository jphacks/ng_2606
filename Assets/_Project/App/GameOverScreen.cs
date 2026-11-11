using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// ゲームオーバー。ハートが 0 になったとき。空っぽのハートと、しょんぼりしたキャラ。
    /// 「もういちど」が、いちばん大きい。撮ったばかりのステージなら、「コースを なおす」も出す（たしかめへ戻る）。
    /// </summary>
    public class GameOverScreen : AppScreen
    {
        AppButton fix;
        AppButton list;

        protected override void Build()
        {
            UiKit.Background(Rect, "Back", Theme.Hex("2E3A5E"), Theme.Navy);
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            var title = UiKit.Label(safe, AppText.GameOverTitle, Theme.Hero, Color.white, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1800f, 260f));

            hearts = HeartRow.Create(safe, "Hearts", 120f);
            UiKit.Place(hearts.Rect, new Vector2(0.5f, 1f), new Vector2(0f, -400f), hearts.Rect.sizeDelta);
            hearts.Set(0);

            hint = UiKit.Label(safe, AppText.GameOverHint, Theme.H2, Theme.WithAlpha(Color.white, 0.85f), TextAlignmentOptions.Center, false, "Hint");
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1400f, 100f));

            // しょんぼりしたキャラ（目が下を向いている）
            var body = UiKit.Panel(safe, "Hero", Theme.Blue, 60f, shadow: true);
            UiKit.Place(body.rectTransform, new Vector2(0.18f, 0.42f), Vector2.zero, new Vector2(150f, 200f));
            body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            body.raycastTarget = false;
            foreach (var dx in new[] { 0.3f, 0.7f })
            {
                var eye = UiKit.Disc(body.rectTransform, "Eye", Color.white, 44f);
                UiKit.Place(eye.rectTransform, new Vector2(dx, 0.62f), Vector2.zero, eye.rectTransform.sizeDelta);
                var pupil = UiKit.Disc(eye.rectTransform, "Pupil", Theme.Ink, 22f);
                UiKit.Place(pupil.rectTransform, new Vector2(0.5f, 0.28f), Vector2.zero, pupil.rectTransform.sizeDelta);
            }

            var again = UiKit.Button(safe, AppText.GameOverRetry, UiKit.ButtonStyle.Primary, new Vector2(820f, Theme.ButtonHeight),
                () => Context.Flow.PlayAgain(), "Again", Theme.H1);
            UiKit.Place(again.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 240f), again.Rect.sizeDelta);

            // 「いちらんへ」の場所は、「コースを なおす」を出すかどうかで決める（OnEnter）
            list = UiKit.Button(safe, AppText.GameOverList, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                () => Context.Flow.BackToList(), "List", Theme.Body);
            fix = UiKit.Button(safe, AppText.GameOverFix, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                () => Context.Router.Back(), "Fix", Theme.Body);
            UiKit.Place(fix.Rect, new Vector2(0.5f, 0f), new Vector2(300f, 60f), fix.Rect.sizeDelta);
        }

        HeartRow hearts;
        TMP_Text hint;

        public override void OnEnter(object arg)
        {
            // 理由：ハートが なくなった（ふつう）、しばりの時間切れ・いっぱつ アウト
            hint.text = arg as string ?? AppText.GameOverHint;
            hearts.ShowHealth(Context.Gimmicks.Health.Hearts, Context.Rules.State.Rules); // いっぱつ アウトなら、1 つだけ
            var canFix = Context.Flow.HasNewStage && !Context.Flow.PlayingSaved;
            fix.gameObject.SetActive(canFix);
            // 「コースを なおす」も出すときは、左右に並べる。出さないときは、「いちらんへ」を真ん中に（2026-11-11：真ん中に置いたまま、重なっていた）
            UiKit.Place(list.Rect, new Vector2(0.5f, 0f), new Vector2(canFix ? -300f : 0f, 60f), list.Rect.sizeDelta);
        }

        public override void OnBack() => Context.Flow.BackToList();
    }
}
