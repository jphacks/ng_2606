using DeskStage.Gameplay;
using DeskStage.Merge;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 遊んでいるときの音（2026-11-11）：
    ///  ・足音：立体のキャラの足が地面に着いたとき（CharacterLimbs.FootDown。足を振るのと同じ数え方なので、足の動きとずれない）
    ///  ・ジャンプ：キャラが跳んだとき（CharacterMotor.Jumped）。空中でもう一度跳んだ（2 段ジャンプ）ときは、高い音
    /// ゴールの音は、クリアの画面が出るときに鳴らす（ClearScreen）。音そのものは UiSound が作る。
    /// 遊ぶ画面のときだけ鳴らす。
    /// </summary>
    public class GameSounds : MonoBehaviour
    {
        AppRouter router;
        GameFlow flow;
        CharacterMotor hooked;

        public void Set(AppRouter appRouter) => router = appRouter;

        void OnEnable()
        {
            CharacterLimbs.FootDown += OnFootDown;
            GameEvents.GameStarted += OnGameStarted;
        }

        void OnDisable()
        {
            CharacterLimbs.FootDown -= OnFootDown;
            GameEvents.GameStarted -= OnGameStarted;
            Hook(null);
        }

        bool Playing => router != null && router.Current == AppScreenId.Play;

        void OnGameStarted()
        {
            if (flow == null) flow = FindFirstObjectByType<GameFlow>();
            Hook(flow != null ? flow.Motor : null);
        }

        // キャラの「跳んだ」の知らせをつなぐ（キャラが替わったら、つなぎ直す）
        void Hook(CharacterMotor motor)
        {
            if (hooked == motor) return;
            if (hooked != null) hooked.Jumped -= OnJumped;
            hooked = motor;
            if (hooked != null) hooked.Jumped += OnJumped;
        }

        void OnJumped(bool air)
        {
            if (Playing) UiSound.Play(air ? UiSound.Cue.AirJump : UiSound.Cue.Jump);
        }

        void OnFootDown(bool left)
        {
            if (Playing) UiSound.Play(left ? UiSound.Cue.StepLeft : UiSound.Cue.StepRight);
        }
    }
}
