using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 隣り合う面の高さの差の扱い。D の印（緑・赤）も、この3つの言葉で受け取る。
    /// </summary>
    public enum StepKind
    {
        /// <summary>歩いて登れる（印なし）</summary>
        Walk,
        /// <summary>ジャンプで登れる（緑の印）</summary>
        Jump,
        /// <summary>登れない壁（赤の印）</summary>
        Wall,
    }

    /// <summary>
    /// 箱の高さの決まり（Docs/StepRules.md）。高さの差から Walk、Jump、Wall を決める計算だけを持つ。
    /// 境目の値は CharacterSettings（Step 2）から渡す。
    /// </summary>
    public class StepRules
    {
        /// <summary>歩ける段の上限の初期値（m）。Step Offset と同じ 0.8cm</summary>
        public const float DefaultWalkMax = 0.008f;

        /// <summary>跳べる段の上限の初期値（m）。ジャンプの高さ 5cm − 足の余裕 0.5cm</summary>
        public const float DefaultJumpMax = 0.045f;

        /// <summary>箱の高さの下限（m）。B の箱づくりと同じ</summary>
        public const float MinBoxHeight = 0.005f;

        /// <summary>箱と箱のすき間の下限（m）。キャラの太さ。これ未満のすき間は B がなくす</summary>
        public const float MinGap = 0.016f;

        // 計算の誤差（0.01mm）で境目ちょうどの段が隣の扱いにずれないための余裕
        const float Tolerance = 0.00001f;

        /// <summary>この差未満なら Walk（m）</summary>
        public float WalkMax { get; }

        /// <summary>この差以下なら Jump、超えると Wall（m）</summary>
        public float JumpMax { get; }

        public StepRules(float walkMax = DefaultWalkMax, float jumpMax = DefaultJumpMax)
        {
            WalkMax = walkMax;
            JumpMax = jumpMax;
        }

        /// <summary>
        /// 隣の面との高さの差（m）から扱いを決める。上りと下りは区別せず、差の大きさだけを見る。
        /// </summary>
        public StepKind Classify(float heightDifference)
        {
            float diff = Mathf.Abs(heightDifference);
            if (diff < WalkMax - Tolerance) return StepKind.Walk;
            if (diff <= JumpMax + Tolerance) return StepKind.Jump;
            return StepKind.Wall;
        }

        /// <summary>2つの面の高さ（m）から扱いを決める。</summary>
        public StepKind Classify(float heightA, float heightB)
        {
            return Classify(heightB - heightA);
        }
    }
}
