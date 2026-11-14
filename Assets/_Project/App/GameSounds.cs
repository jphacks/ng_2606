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
    /// 2026-11-15 に足した音（聞き比べて、えらんだ）：
    ///  ・ワープ「ひゅいーん＋きらきら」（GimmickSystem.Warped）
    ///  ・落ちた「ひゅ〜〜」（GameEvents.Fell）
    ///  ・炎に当たった「パチパチッ」、敵に当たった「びびっ」（GimmickSystem.Damaged。どちらで減ったかは LastDamageWasEnemy）
    ///  ・水に流されている間の「ちょろちょろ」（キャラに、流される速さ ExternalVelocity があるあいだ、くり返し鳴らす）
    /// ゴールの音は、クリアの画面が出るときに鳴らす（ClearScreen）。ゲームオーバーの「ぷわわ〜ん」は、しばりの時間切れなども含めて、AppFlow.ShowGameOver で鳴らす。遊んでいるときの BGM は AppFlow が切り替える。音そのものは UiSound が作る。
    /// 遊ぶ画面のときだけ鳴らす。
    /// </summary>
    public class GameSounds : MonoBehaviour
    {
        // これより速く流されていたら、水の音を鳴らす（m/秒）
        const float WaterSoundSpeed = 0.005f;

        AppRouter router;
        GimmickSystem gimmicks;
        GameFlow flow;
        CharacterMotor hooked;

        public void Set(AppRouter appRouter, GimmickSystem gimmickSystem)
        {
            router = appRouter;
            if (gimmicks != null) Unhook(gimmicks);
            gimmicks = gimmickSystem;
            if (isActiveAndEnabled && gimmicks != null) HookGimmicks(gimmicks);
        }

        void OnEnable()
        {
            CharacterLimbs.FootDown += OnFootDown;
            GameEvents.GameStarted += OnGameStarted;
            GameEvents.Fell += OnFell;
            if (gimmicks != null) HookGimmicks(gimmicks);
        }

        void OnDisable()
        {
            CharacterLimbs.FootDown -= OnFootDown;
            GameEvents.GameStarted -= OnGameStarted;
            GameEvents.Fell -= OnFell;
            if (gimmicks != null) Unhook(gimmicks);
            Hook(null);
            UiSound.SetWater(false);
        }

        void HookGimmicks(GimmickSystem g)
        {
            Unhook(g);
            g.Warped += OnWarped;
            g.Damaged += OnDamaged;
        }

        void Unhook(GimmickSystem g)
        {
            g.Warped -= OnWarped;
            g.Damaged -= OnDamaged;
        }

        bool Playing => router != null && router.Current == AppScreenId.Play;

        void Update()
        {
            var motor = flow != null ? flow.Motor : null;
            var flowing = Playing && motor != null && flow.State == GameState.Playing && motor.ExternalVelocity.magnitude > WaterSoundSpeed;
            UiSound.SetWater(flowing);
        }

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

        void OnWarped()
        {
            if (Playing) UiSound.Play(UiSound.Cue.Warp);
        }

        void OnFell()
        {
            if (Playing) UiSound.Play(UiSound.Cue.Fall);
        }

        void OnDamaged(int hearts)
        {
            // ハートが 0 になったときは、ゲームオーバーの音だけを鳴らす（当たった音と重ねない）
            if (!Playing || hearts <= 0) return;
            UiSound.Play(gimmicks != null && gimmicks.LastDamageWasEnemy ? UiSound.Cue.EnemyHit : UiSound.Cue.FireHit);
        }
    }
}
