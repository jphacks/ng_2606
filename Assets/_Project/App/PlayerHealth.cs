using System;

namespace DeskStage.App
{
    /// <summary>
    /// ハート（体力）。3 つ。炎の箱に乗ると 1 つ減り、しばらく（1.5 秒）は、続けて減らない。緑の箱の上では、回復する。0 でゲームオーバー。
    /// 画面や 3D に触れない計算だけを持つ（EditMode テストで確かめる）。時刻は、呼ぶ側が渡す。
    /// </summary>
    public class PlayerHealth
    {
        /// <summary>ハートの数の最大。</summary>
        public const int Max = 3;

        /// <summary>減ったあと、続けて減らない時間（秒）。</summary>
        public const float InvulnerableSeconds = 1.5f;

        float invulnerableUntil = float.NegativeInfinity;

        public int Hearts { get; private set; } = Max;

        public bool IsDead => Hearts <= 0;

        /// <summary>減ったあとの、まもられている間か。</summary>
        public bool IsInvulnerable(float now) => now < invulnerableUntil;

        /// <summary>ハートを 1 つ減らす。まもられている間や、もう 0 のときは、減らさず false。</summary>
        public bool TryDamage(float now)
        {
            if (IsDead || IsInvulnerable(now))
            {
                return false;
            }
            Hearts--;
            invulnerableUntil = now + InvulnerableSeconds;
            return true;
        }

        /// <summary>ハートを回復する（最大まで）。もう最大のとき、0（ゲームオーバー）のときは、false。</summary>
        public bool Heal(int amount = 1)
        {
            if (IsDead || Hearts >= Max || amount <= 0)
            {
                return false;
            }
            Hearts = Math.Min(Max, Hearts + amount);
            return true;
        }

        /// <summary>はじめから（ゲームを始める、やり直すとき）。</summary>
        public void Reset()
        {
            Hearts = Max;
            invulnerableUntil = float.NegativeInfinity;
        }
    }
}
