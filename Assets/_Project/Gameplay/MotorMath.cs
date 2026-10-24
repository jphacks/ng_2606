using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// CharacterMotor の計算部分（C_gameplay.md の Step 4）。MonoBehaviour から切り離して EditMode でテストする。
    /// </summary>
    public static class MotorMath
    {
        /// <summary>接地中に戻す小さな下向きの速さ（m/秒）。接地が切れたりついたりしないため</summary>
        public const float GroundStickSpeed = 0.05f;

        /// <summary>
        /// 水平の速さ（m/秒）。スティックの倒した量（長さ1まで）を掛ける。
        /// </summary>
        public static Vector3 HorizontalVelocity(Vector3 direction, float walkSpeed)
        {
            return Vector3.ClampMagnitude(new Vector3(direction.x, 0f, direction.z), 1f) * walkSpeed;
        }

        /// <summary>接地していて下向きに動いているなら、小さな下向きの値に戻す。</summary>
        public static float StickToGround(float verticalSpeed, bool grounded)
        {
            return grounded && verticalSpeed < 0f ? -GroundStickSpeed : verticalSpeed;
        }

        /// <summary>重力で上下の速さを減らす。落下の速さは上限まで。</summary>
        public static float ApplyGravity(float verticalSpeed, float gravity, float maxFallSpeed, float deltaTime)
        {
            return Mathf.Max(verticalSpeed - gravity * deltaTime, -maxFallSpeed);
        }
    }

    /// <summary>
    /// ジャンプの先行入力と、落ちた直後のジャンプの猶予を受け持つ。
    /// 押されてから buffer 秒以内、かつ接地していたのが coyote 秒以内ならジャンプする。
    /// </summary>
    public class JumpTimer
    {
        float lastGroundedTime = float.NegativeInfinity;
        float lastJumpPressedTime = float.NegativeInfinity;

        public void RecordGrounded(float time) => lastGroundedTime = time;

        public void RecordJumpPressed(float time) => lastJumpPressedTime = time;

        /// <summary>ジャンプしてよければ true を返し、2つの時刻を古い値に戻す（同じ入力で2回跳ばないように）。</summary>
        public bool TryConsume(float now, float bufferTime, float coyoteTime)
        {
            if (now - lastJumpPressedTime > bufferTime || now - lastGroundedTime > coyoteTime)
            {
                return false;
            }
            lastGroundedTime = float.NegativeInfinity;
            lastJumpPressedTime = float.NegativeInfinity;
            return true;
        }
    }
}
