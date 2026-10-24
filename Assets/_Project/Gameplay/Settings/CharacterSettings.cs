using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// キャラとカメラで調整する数値（C_gameplay.md の Step 2）。
    /// 初期値は手順書の値。遊びながら Inspector で変える。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSettings", menuName = "DeskStage/C/キャラの設定")]
    public class CharacterSettings : ScriptableObject
    {
        [Header("Character Controller")]

        [Tooltip("身長（m）。3cm")]
        [Min(0f)] public float height = 0.03f;

        [Tooltip("半径（m）。太さ1.6cm")]
        [Min(0f)] public float radius = 0.008f;

        [Tooltip("体の中心の高さ（m）。足元がキャラの原点になるよう、身長の半分")]
        public float centerY = 0.015f;

        [Tooltip("当たり判定の余白（m）。初期値 0.08 のままだと完全に浮くので、必ずこの値にする")]
        [Min(0f)] public float skinWidth = 0.0008f;

        [Tooltip("自動で登れる段差（m）。0.8cm")]
        [Min(0f)] public float stepOffset = 0.008f;

        [Tooltip("登れる坂の角度（度）")]
        [Range(0f, 90f)] public float slopeLimit = 45f;

        [Tooltip("これより短い移動は無視する（m）")]
        [Min(0f)] public float minMoveDistance = 0f;

        [Header("移動")]

        [Tooltip("歩く速さ（m/秒）")]
        [Min(0f)] public float walkSpeed = 0.15f;

        [Tooltip("重力（m/秒²）。速すぎたら下げる")]
        [Min(0f)] public float gravity = 9.8f;

        [Tooltip("ジャンプの高さ（m）。変えたら跳べる段の上限と Docs/StepRules.md も直す")]
        [Min(0f)] public float jumpHeight = 0.04f;

        [Tooltip("着地の前に押したジャンプを覚えておく時間（秒）")]
        [Min(0f)] public float jumpBufferTime = 0.1f;

        [Tooltip("足場から落ちた直後でもジャンプできる時間（秒）")]
        [Min(0f)] public float coyoteTime = 0.1f;

        [Tooltip("落下の速さの上限（m/秒）")]
        [Min(0f)] public float maxFallSpeed = 1f;

        [Tooltip("向きを変える速さ（度/秒）")]
        [Min(0f)] public float turnSpeed = 720f;

        [Header("箱の高さの決まり（StepRules の境目）")]

        [Tooltip("跳べる段の上限（m）。ジャンプの高さ＋丸い足元で乗り上げる分 約0.5cm（Step 8 で測定）")]
        [Min(0f)] public float jumpableStepMax = StepRules.DefaultJumpMax;

        [Tooltip("歩ける段の上限（m）。Step Offset と同じ")]
        [Min(0f)] public float walkableStepMax = StepRules.DefaultWalkMax;

        [Header("カメラ")]

        [Tooltip("カメラの距離の初期値（m）")]
        [Min(0f)] public float cameraDistance = 0.35f;

        [Tooltip("カメラの距離の下限（m）")]
        [Min(0f)] public float cameraDistanceMin = 0.20f;

        [Tooltip("カメラの距離の上限（m）")]
        [Min(0f)] public float cameraDistanceMax = 0.80f;

        [Tooltip("カメラの見下ろす角度の初期値（度）")]
        [Range(0f, 90f)] public float cameraPitch = 40f;

        [Tooltip("カメラの見下ろす角度の下限（度）")]
        [Range(0f, 90f)] public float cameraPitchMin = 20f;

        [Tooltip("カメラの見下ろす角度の上限（度）")]
        [Range(0f, 90f)] public float cameraPitchMax = 80f;

        /// <summary>ジャンプの初速（m/秒）。「2×重力×高さ」の平方根。初期値で約 0.89</summary>
        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);

        /// <summary>この設定の境目を使う StepRules を作る。</summary>
        public StepRules CreateStepRules()
        {
            return new StepRules(walkableStepMax, jumpableStepMax);
        }

        /// <summary>Character Controller にこの設定の値を入れる。</summary>
        public void ApplyTo(CharacterController controller)
        {
            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(0f, centerY, 0f);
            controller.skinWidth = skinWidth;
            controller.stepOffset = stepOffset;
            controller.slopeLimit = slopeLimit;
            controller.minMoveDistance = minMoveDistance;
        }
    }
}
