using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 立体のキャラの手足の角度（2026-11-10）。CharacterLimbs が毎フレーム使う。MonoBehaviour から切り離して、EditMode テストで確かめる。
    ///  ・歩く：腕は肩、足は付け根を軸に、前後に振る。右腕と左足、左腕と右足が同じ向き（人が歩くときと同じ）。振る大きさは、歩く速さに合わせる
    ///  ・止まる：体を少し上下させる（息をしているように）
    ///  ・跳んでいる（上がっている）：両腕を横から上げ、足をそろえる
    ///  ・落ちている：手足を速く、ばたばたさせる
    /// 角度は度。Swing は前後（+ で前へ）、Raise は横へ上げる角度。
    /// </summary>
    public static class LimbPose
    {
        public struct Pose
        {
            public float LeftArmSwing;
            public float RightArmSwing;
            public float LeftLegSwing;
            public float RightLegSwing;
            public float ArmRaise;
            public float Bob; // 体の上下（身長に対する割合）
        }

        public enum State { Ground, Rising, Falling }

        /// <summary>歩くときに、腕と足を振る最大の角度（度）。</summary>
        public const float ArmSwing = 55f;
        public const float LegSwing = 45f;

        /// <summary>
        /// 足を 1 往復（2 歩）させる間に進む距離（m）。足の長さ legLength（付け根から足の裏まで）と、振る角度 LegSwing から決める：
        /// 1 歩で、足先が前後に 2 × 足の長さ × sin(角度) 動くので、2 歩で その 2 倍。進んだ距離の分だけ足を運べば、足がすべって見えない。
        /// </summary>
        public static float CycleLength(float legLength) => 4f * legLength * Mathf.Sin(LegSwing * Mathf.Deg2Rad);

        /// <summary>
        /// 何歩目か（足音を鳴らす数え方。2026-11-11）。足は、位相が π/2 + kπ のとき、いちばん前に出て地面に着く
        /// （π/2 で右足、3π/2 で左足）。この数が増えたら、足が 1 回着いた。偶数は右足、奇数は左足。
        /// </summary>
        public static int StepIndex(float phase) => Mathf.FloorToInt((phase - Mathf.PI / 2f) / Mathf.PI);

        /// <summary>足音を鳴らす、いちばん小さな振りの大きさ（0〜1）。これより小さい足踏みでは鳴らさない。</summary>
        public const float StepSoundSwing = 0.3f;

        /// <summary>跳んでいる間に、腕を上げる角度（度）。</summary>
        public const float JumpRaise = 130f;

        /// <summary>落ちている間の、ばたばたの大きさ（度）と、上げる角度（度）。</summary>
        public const float FlailSwing = 25f;
        public const float FlailRaise = 70f;

        /// <summary>落ちているとみなす、下向きの速さ（m/秒）。これより遅ければ、跳ぶ・着地の途中とみなす。</summary>
        public const float FallSpeed = 0.35f;

        /// <summary>いまの状態。grounded：地面に立っている。verticalSpeed：上下の速さ（m/秒、+ が上）。</summary>
        public static State StateOf(bool grounded, float verticalSpeed)
        {
            if (grounded) return State.Ground;
            return verticalSpeed < -FallSpeed ? State.Falling : State.Rising;
        }

        /// <summary>
        /// 姿勢。phase：歩きの位相（ラジアン。歩いた分だけ進める）。speed01：歩く速さ（0〜1。1 が最高の速さ）。
        /// time：時刻（秒。止まっているときの息と、ばたばたに使う）。
        /// </summary>
        public static Pose Evaluate(State state, float phase, float speed01, float time)
        {
            var pose = new Pose();
            switch (state)
            {
                case State.Rising:
                    pose.ArmRaise = JumpRaise;
                    return pose;
                case State.Falling:
                    var f = Mathf.Sin(time * 22f);
                    pose.ArmRaise = FlailRaise;
                    pose.LeftArmSwing = f * FlailSwing;
                    pose.RightArmSwing = -f * FlailSwing;
                    pose.LeftLegSwing = -f * FlailSwing;
                    pose.RightLegSwing = f * FlailSwing;
                    return pose;
            }
            var s = Mathf.Clamp01(speed01);
            var w = Mathf.Sin(phase);
            pose.LeftArmSwing = w * ArmSwing * s;
            pose.RightArmSwing = -w * ArmSwing * s;
            pose.LeftLegSwing = -w * LegSwing * s;
            pose.RightLegSwing = w * LegSwing * s;
            // 歩くと、1 歩ごとに少し上下する。止まっているときは、ゆっくり息をする
            pose.Bob = s > 0.05f ? Mathf.Abs(Mathf.Sin(phase)) * 0.04f * s : (Mathf.Sin(time * 2.4f) * 0.5f + 0.5f) * 0.015f;
            return pose;
        }
    }
}
