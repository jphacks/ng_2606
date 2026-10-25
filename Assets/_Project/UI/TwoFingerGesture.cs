using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 2本指の操作を読む。計算だけの普通のクラス（Step 11）。
    /// つまむ・広げる（間隔）と、ひねる（角度）のうち、最初に大きく変わった方だけを使う（決まり）。
    /// </summary>
    public class TwoFingerGesture
    {
        public enum Mode
        {
            Undecided,
            Pinch,
            Twist,
        }

        /// <summary>間隔がこの割合だけ変わったら「つまむ」と決める</summary>
        public const float PinchThreshold = 0.08f;
        /// <summary>角度がこの度数だけ変わったら「ひねる」と決める</summary>
        public const float TwistThreshold = 10f;

        Vector2 startA;
        Vector2 startB;

        public Mode Current { get; private set; }

        /// <summary>つまむ・広げるの倍率（1 で変わらない）。Pinch のときだけ意味がある</summary>
        public float Scale { get; private set; } = 1f;

        /// <summary>ひねった角度（度。上から見て時計回りが正）。Twist のときだけ意味がある</summary>
        public float Angle { get; private set; }

        public void Begin(Vector2 a, Vector2 b)
        {
            startA = a;
            startB = b;
            Current = Mode.Undecided;
            Scale = 1f;
            Angle = 0f;
        }

        public void Update(Vector2 a, Vector2 b)
        {
            var d0 = Vector2.Distance(startA, startB);
            var d1 = Vector2.Distance(a, b);
            var ratio = d0 > 0f ? d1 / d0 : 1f;
            // 画面の上で反時計回り＝上から見た机の上では反時計回り。箱の向き（Y軸まわり）は時計回りが正なので符号を反転する
            var angle = -Vector2.SignedAngle(startB - startA, b - a);

            if (Current == Mode.Undecided)
            {
                var pinchAmount = Mathf.Abs(ratio - 1f) / PinchThreshold;
                var twistAmount = Mathf.Abs(angle) / TwistThreshold;
                if (pinchAmount >= 1f || twistAmount >= 1f)
                {
                    Current = pinchAmount >= twistAmount ? Mode.Pinch : Mode.Twist;
                }
            }
            Scale = Current == Mode.Pinch ? ratio : 1f;
            Angle = Current == Mode.Twist ? angle : 0f;
        }
    }
}
