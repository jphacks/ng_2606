using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// しばり（Docs/specs/course-rules）の決まりの計算。ジャンプを数える、跳んでよいか、時間切れか。
    /// 画面とキャラに触れない（EditMode でテストする）。キャラには、App の CourseRuleSystem が、跳んでよいかを渡す。
    /// </summary>
    public class CourseRuleState
    {
        public CourseRules Rules { get; private set; } = new CourseRules();

        /// <summary>このプレイで使ったジャンプの数（地面からも空中からも 1 回）。</summary>
        public int JumpsUsed { get; private set; }

        /// <summary>遊び始め・やり直しのとき。使ったジャンプの数を 0 に戻す。</summary>
        public void Reset(CourseRules rules)
        {
            Rules = rules ?? new CourseRules();
            JumpsUsed = 0;
        }

        /// <summary>跳んでよいか。air は空中（2 段ジャンプ）のとき。</summary>
        public bool CanJump(bool air)
        {
            if (air && Rules.noDoubleJump)
            {
                return false;
            }
            return Rules.maxJumps < 0 || JumpsUsed < Rules.maxJumps;
        }

        /// <summary>跳んだ。</summary>
        public void OnJumped() => JumpsUsed++;

        /// <summary>ジャンプの残りの回数。むせいげんなら -1。</summary>
        public int JumpsLeft => Rules.maxJumps < 0 ? -1 : Mathf.Max(0, Rules.maxJumps - JumpsUsed);

        public bool HasTimeLimit => Rules.timeLimitSeconds > 0f;

        /// <summary>プレイの時間（一時停止中は数えない）が、制限時間に達したか。</summary>
        public bool IsTimeUp(float elapsedSeconds) => HasTimeLimit && elapsedSeconds >= Rules.timeLimitSeconds;

        /// <summary>残りの秒数。制限時間が無ければ -1。</summary>
        public float SecondsLeft(float elapsedSeconds) => HasTimeLimit ? Mathf.Max(0f, Rules.timeLimitSeconds - elapsedSeconds) : -1f;

        /// <summary>しばりが 1 つでもあるか。</summary>
        public static bool HasAny(CourseRules rules)
        {
            return rules != null && (rules.maxJumps >= 0 || rules.noDoubleJump || rules.oneHit || rules.timeLimitSeconds > 0f);
        }
    }
}
