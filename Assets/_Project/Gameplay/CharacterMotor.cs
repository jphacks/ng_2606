using System;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 入力を受けて歩き、跳び、落ちる（C_gameplay.md の Step 4）。
    /// 毎フレーム速さを計算し、最後に1回だけ Character Controller を動かす。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        public CharacterSettings settings;

        /// <summary>一時停止中は計算を丸ごと飛ばす</summary>
        public bool Paused { get; set; }

        /// <summary>
        /// スティックの値を移動の向きに直す。Step 7 の CameraRelativeInput が入れる。
        /// 入っていなければ世界の前後左右（スティックの上＝Z の正）。
        /// </summary>
        public Func<Vector2, Vector3> DirectionProvider { get; set; }

        CharacterController controller;
        TemporaryControls controls;
        readonly JumpTimer jumpTimer = new JumpTimer();
        float verticalSpeed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            settings.ApplyTo(controller);
            controls = new TemporaryControls();
        }

        void OnEnable() => controls.Enable();

        void OnDisable() => controls.Disable();

        void OnDestroy() => controls.Dispose();

        void Update()
        {
            if (Paused)
            {
                return;
            }

            float dt = Time.deltaTime;
            float now = Time.time;

            // 1・2：入力と水平の速さ
            Vector2 stick = controls.Move.ReadValue<Vector2>();
            Vector3 direction = DirectionProvider != null ? DirectionProvider(stick) : new Vector3(stick.x, 0f, stick.y);
            Vector3 horizontal = MotorMath.HorizontalVelocity(direction, settings.walkSpeed);

            // 3：接地中は小さな下向きの値に戻す
            bool grounded = controller.isGrounded;
            verticalSpeed = MotorMath.StickToGround(verticalSpeed, grounded);

            // 4：最後に接地した時刻と、最後にジャンプが押された時刻
            if (grounded)
            {
                jumpTimer.RecordGrounded(now);
            }
            if (controls.Jump.WasPressedThisFrame())
            {
                jumpTimer.RecordJumpPressed(now);
            }

            // 5：ジャンプ
            if (jumpTimer.TryConsume(now, settings.jumpBufferTime, settings.coyoteTime))
            {
                verticalSpeed = settings.JumpVelocity;
            }

            // 6：重力
            verticalSpeed = MotorMath.ApplyGravity(verticalSpeed, settings.gravity, settings.maxFallSpeed, dt);

            // 7：水平と上下を合わせて1回だけ動かす
            controller.Move((horizontal + Vector3.up * verticalSpeed) * dt);

            // 8：動いているときは進む方向へなめらかに向く
            if (horizontal.sqrMagnitude > 1e-8f)
            {
                var target = Quaternion.LookRotation(horizontal, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, settings.turnSpeed * dt);
            }
        }
    }
}
