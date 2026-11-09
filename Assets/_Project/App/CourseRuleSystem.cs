using System;
using System.Collections.Generic;
using DeskStage.Gameplay;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// しばりの係（Docs/specs/course-rules）。遊び始めるたびに、いまのステージのしばりを読んで、
    /// キャラに「跳んでよいか」を渡し、ジャンプを数える。時間切れと、いっぱつ アウトで、GameOver（理由の文）を出す。
    /// 決まりの計算は Gameplay の CourseRuleState。ここは、つなぐだけ。
    /// </summary>
    public class CourseRuleSystem : MonoBehaviour
    {
        public CourseRuleState State { get; } = new CourseRuleState();

        /// <summary>しばりを破った（引数は、ゲームオーバーの画面に出す理由）。</summary>
        public event Action<string> GameOver;

        GimmickSystem gimmicks;
        GameFlow flow;
        CharacterMotor hooked;
        bool over;

        /// <summary>AppRoot が、GimmickSystem を渡す（ダメージを受けたかを知るため）。</summary>
        public void Set(GimmickSystem gimmickSystem)
        {
            if (gimmicks != null) gimmicks.Damaged -= OnDamaged;
            gimmicks = gimmickSystem;
            if (gimmicks != null) gimmicks.Damaged += OnDamaged;
        }

        void OnEnable() => GameEvents.GameStarted += OnGameStarted;

        void OnDisable()
        {
            GameEvents.GameStarted -= OnGameStarted;
            Hook(null);
        }

        void OnDestroy()
        {
            if (gimmicks != null) gimmicks.Damaged -= OnDamaged;
        }

        void OnGameStarted()
        {
            State.Reset(StageContext.CurrentStage != null ? StageContext.CurrentStage.rules : null);
            over = false;
            if (flow == null) flow = FindFirstObjectByType<GameFlow>();
            Hook(flow != null ? flow.Motor : null);
        }

        // キャラに、跳んでよいかを聞く口と、跳んだときの知らせをつなぐ（キャラが替わったら、つなぎ直す）
        void Hook(CharacterMotor motor)
        {
            if (hooked == motor) return;
            if (hooked != null)
            {
                hooked.JumpAllowed = null;
                hooked.Jumped -= OnJumped;
            }
            hooked = motor;
            if (hooked != null)
            {
                hooked.JumpAllowed = State.CanJump;
                hooked.Jumped += OnJumped;
            }
        }

        void OnJumped(bool air) => State.OnJumped();

        void OnDamaged(int remaining)
        {
            if (!over && State.Rules.oneHit && Playing)
            {
                over = true;
                GameOver?.Invoke(AppText.GameOverOneHit);
            }
        }

        void Update()
        {
            if (!over && Playing && State.IsTimeUp(flow.ElapsedSeconds))
            {
                over = true;
                GameOver?.Invoke(AppText.GameOverTimeUp);
            }
        }

        bool Playing => flow != null && flow.State == GameState.Playing;

        /// <summary>遊ぶ画面に出す、しばりと残り（無ければ空）。</summary>
        public string PlayText()
        {
            var r = State.Rules;
            if (!CourseRuleState.HasAny(r)) return "";
            var parts = new List<string>();
            // ジャンプの残りの回数は、ハートの下の印で見せる（PlayScreen）。ここでは、きんしだけ文字で出す
            if (r.maxJumps == 0) parts.Add(AppText.RulesJump + " " + AppText.RulesJumpNone);
            if (r.noDoubleJump && r.maxJumps != 0) parts.Add(AppText.RulesPlayNoDouble);
            if (r.oneHit) parts.Add(AppText.RulesOneHit);
            if (State.HasTimeLimit && flow != null) parts.Add(string.Format(AppText.RulesPlayTimeLeft, Mathf.CeilToInt(State.SecondsLeft(flow.ElapsedSeconds))));
            return string.Join("　", parts);
        }

        /// <summary>しばりの短いまとめ（クリアの画面など）。しばりが無ければ空。</summary>
        public static string Summary(CourseRules r)
        {
            if (!CourseRuleState.HasAny(r)) return "";
            var parts = new List<string>();
            if (r.maxJumps == 0) parts.Add(AppText.RulesJump + " " + AppText.RulesJumpNone);
            else if (r.maxJumps > 0) parts.Add(AppText.RulesJump + " " + string.Format(AppText.RulesJumpCount, r.maxJumps));
            if (r.noDoubleJump && r.maxJumps != 0) parts.Add(AppText.RulesPlayNoDouble);
            if (r.oneHit) parts.Add(AppText.RulesOneHit);
            if (r.timeLimitSeconds > 0f) parts.Add(string.Format(AppText.RulesSeconds, Mathf.RoundToInt(r.timeLimitSeconds)));
            return string.Join("・", parts);
        }
    }
}
