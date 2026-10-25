using DeskStage.Shared;
using DeskStage.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// あそぶ。3D を見ながら、左下のスティックで動かし、右下のボタンでジャンプする。画面のどこかをなぞると、視点が動く（D の ViewGesture）。
    /// 左上の「いちじていし」で止めて、「つづける」「もういちど」「やめる」を選べる。タイムは上に出す。
    /// 「やめる」は、来た画面（たしかめ か いちらん）へ戻る。
    /// </summary>
    public class PlayScreen : AppScreen
    {
        const float StickDiameter = 300f;
        const float JumpDiameter = 250f;

        TMP_Text timer;
        HeartRow hearts;
        Image damageFlash;
        float flashTime = 1f;
        RectTransform pauseCard;
        bool paused;

        protected override void Build()
        {
            // 視点の操作の板（スティック・ボタンより奥）
            var gestureImage = UiParts.Clear(Rect, "ViewGesture", true);
            var gesture = gestureImage.gameObject.AddComponent<ViewGesture>();
            gesture.SetSettings(Context.ViewGestureSettings);

            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));

            // 上：いちじていし と タイム
            var pause = UiKit.Button(safe, AppText.PlayPause, UiKit.ButtonStyle.Overlay, new Vector2(440f, Theme.SmallButtonHeight),
                () => SetPaused(true), "Pause", Theme.Body);
            UiKit.Place(pause.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -40f), pause.Rect.sizeDelta);

            var timerBack = UiKit.Panel(safe, "TimerBack", Theme.WithAlpha(Theme.Navy, 0.78f), 70f);
            UiKit.Place(timerBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(480f, 130f));
            timer = UiKit.Label(timerBack.rectTransform, FlowRules.FormatTime(0f), Theme.H1, Color.white, TextAlignmentOptions.Center, true, "Timer");
            UiKit.Fill(timer.rectTransform);

            // 右上：ハート（体力）
            var heartBack = UiKit.Panel(safe, "HeartBack", Theme.WithAlpha(Theme.Navy, 0.78f), 70f);
            UiKit.Place(heartBack.rectTransform, new Vector2(1f, 1f), new Vector2(-Theme.Margin, -40f), new Vector2(480f, 130f));
            hearts = HeartRow.Create(heartBack.rectTransform, "Hearts", 84f);
            UiKit.Place(hearts.Rect, new Vector2(0.5f, 0.5f), Vector2.zero, hearts.Rect.sizeDelta);
            hearts.Set(PlayerHealth.Max);

            // 左下：スティック（外周の中で取っ手を動かすと、仮想ゲームパッドの左スティックになる）
            var stickBase = UiKit.Disc(safe, "Stick", Theme.WithAlpha(Color.white, 0.55f), StickDiameter);
            stickBase.raycastTarget = true; // 外周の上で始まった指も受け止め、視点の操作に渡さない
            UiKit.Place(stickBase.rectTransform, new Vector2(0f, 0f), new Vector2(Theme.Margin + 40f, 80f), stickBase.rectTransform.sizeDelta);
            var knobImage = UiKit.Disc(stickBase.rectTransform, "Knob", Color.white, StickDiameter * 0.5f);
            knobImage.raycastTarget = true;
            UiKit.Place(knobImage.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, knobImage.rectTransform.sizeDelta);
            knobImage.gameObject.SetActive(false);
            var stick = knobImage.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = StickDiameter * 0.5f;
            knobImage.gameObject.SetActive(true);

            // 右下：ジャンプ（仮想ゲームパッドの下のボタン）
            var jump = UiKit.Disc(safe, "Jump", Theme.Orange, JumpDiameter);
            jump.raycastTarget = true;
            UiKit.Place(jump.rectTransform, new Vector2(1f, 0f), new Vector2(-Theme.Margin - 40f, 100f), jump.rectTransform.sizeDelta);
            jump.gameObject.SetActive(false);
            var jumpButton = jump.gameObject.AddComponent<OnScreenButton>();
            jumpButton.controlPath = "<Gamepad>/buttonSouth";
            jump.gameObject.SetActive(true);
            jump.gameObject.AddComponent<PressScale>();
            var jumpLabel = UiKit.FitLabel(jump.rectTransform, AppText.PlayJump, 46f, Theme.Ink, TextAlignmentOptions.Center, true, "Label");
            UiKit.Fill(jumpLabel.rectTransform);
            jumpLabel.rectTransform.offsetMin = new Vector2(20f, 0f);
            jumpLabel.rectTransform.offsetMax = new Vector2(-20f, 0f);

            // ダメージを受けたときの、赤い光
            damageFlash = UiParts.Clear(Rect, "DamageFlash", false);
            damageFlash.color = new Color(1f, 0.15f, 0.15f, 0f);

            BuildPause();
        }

        void BuildPause()
        {
            pauseCard = UiKit.Fill(UiKit.NewRect("Pause", Rect));
            var dim = pauseCard.gameObject.AddComponent<Image>();
            dim.color = Theme.WithAlpha(Theme.Navy, 0.6f);

            var card = UiKit.Panel(pauseCard, "Card", Theme.Card, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 800f));
            var title = UiKit.Label(card.rectTransform, AppText.PlayPausedTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1200f, 120f));

            var resume = UiKit.Button(card.rectTransform, AppText.PlayResume, UiKit.ButtonStyle.Primary, new Vector2(900f, Theme.ButtonHeight),
                () => SetPaused(false), "Resume", Theme.H1);
            UiKit.Place(resume.Rect, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), resume.Rect.sizeDelta);
            var retry = UiKit.Button(card.rectTransform, AppText.PlayRetry, UiKit.ButtonStyle.Soft, new Vector2(620f, Theme.ButtonHeight),
                OnRetry, "Retry", Theme.H2);
            UiKit.Place(retry.Rect, new Vector2(0.28f, 0f), new Vector2(0f, 70f), retry.Rect.sizeDelta);
            var quit = UiKit.Button(card.rectTransform, AppText.PlayQuit, UiKit.ButtonStyle.Soft, new Vector2(620f, Theme.ButtonHeight),
                OnQuit, "Quit", Theme.H2);
            UiKit.Place(quit.Rect, new Vector2(0.72f, 0f), new Vector2(0f, 70f), quit.Rect.sizeDelta);
            quit.Label.color = Theme.Red;
            pauseCard.gameObject.SetActive(false);
        }

        public override void OnEnter(object arg)
        {
            paused = false;
            pauseCard.gameObject.SetActive(false);
            timer.text = FlowRules.FormatTime(0f);
            GameEvents.ElapsedTime += OnElapsed;
            GameEvents.CheckpointPassed += OnCheckpoint;
            GameEvents.Fell += OnFell;
            Context.Gimmicks.Damaged += OnDamaged;
            Context.Gimmicks.Healed += OnHealed;
            flashTime = 1f;
            damageFlash.color = new Color(1f, 0.15f, 0.15f, 0f);
            hearts.Set(Context.Gimmicks.Health.Hearts);
        }

        public override void OnExit()
        {
            GameEvents.ElapsedTime -= OnElapsed;
            GameEvents.CheckpointPassed -= OnCheckpoint;
            GameEvents.Fell -= OnFell;
            Context.Gimmicks.Damaged -= OnDamaged;
            Context.Gimmicks.Healed -= OnHealed;
        }

        void Update()
        {
            hearts.Set(Context.Gimmicks.Health.Hearts);
            if (flashTime < 1f)
            {
                flashTime += Time.unscaledDeltaTime / 0.5f;
                damageFlash.color = new Color(1f, 0.15f, 0.15f, Mathf.Clamp01(1f - flashTime) * 0.45f);
            }
        }

        void OnDamaged(int remaining)
        {
            flashTime = 0f;
            UiSound.Play(UiSound.Cue.Error);
            Context.Overlays.Toast(AppText.GimmickHit, AppOverlays.ToastKind.Bad, 1.6f);
        }

        void OnHealed(int remaining)
        {
            UiSound.Play(UiSound.Cue.Success);
            Context.Overlays.Toast(AppText.GimmickHealed, AppOverlays.ToastKind.Good, 1.2f);
        }

        // 端末の戻る操作は、とめる／つづける
        public override void OnBack() => SetPaused(!paused);

        void SetPaused(bool value)
        {
            if (paused == value)
            {
                return;
            }
            paused = value;
            pauseCard.gameObject.SetActive(value);
            Context.Flow.SetPaused(value);
        }

        void OnRetry()
        {
            paused = false;
            pauseCard.gameObject.SetActive(false);
            Context.Flow.RetryPlay();
        }

        void OnQuit()
        {
            Context.Overlays.Confirm(AppText.PlayQuitConfirm, () => Context.Router.Back(), AppText.Yes, AppText.No);
        }

        void OnElapsed(float seconds) => timer.text = FlowRules.FormatTime(seconds);

        void OnCheckpoint(int index)
        {
            UiSound.Play(UiSound.Cue.Success);
            Context.Overlays.Toast(AppText.PlayCheckpoint, AppOverlays.ToastKind.Good, 1.6f);
        }

        void OnFell()
        {
            UiSound.Play(UiSound.Cue.Error);
            Context.Overlays.Toast(AppText.PlayFell, AppOverlays.ToastKind.Info, 1.6f);
        }
    }
}
