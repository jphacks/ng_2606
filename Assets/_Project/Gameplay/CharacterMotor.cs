using System;
using DeskStage.Shared;
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

        /// <summary>瞬間移動したとき。視点のカメラがキャラの背中側に回り直す</summary>
        public event Action Teleported;

        /// <summary>
        /// 外から加わる水平の速さ（m/秒）。水の箱の上で流される分など。毎フレームの動きに足して、1 回だけ Character Controller を動かす
        /// （別に Move を呼ぶと、接地の判定が狂って、ジャンプできなくなる）。使わないときはゼロ。
        /// </summary>
        public Vector3 ExternalVelocity { get; set; }

        /// <summary>このフレームのスティックの値（Step 15 のはさまりの判定で使う）</summary>
        public Vector2 Stick { get; private set; }

        /// <summary>このフレームに進もうとした水平の向き（長さ1。止まっていればゼロ）</summary>
        public Vector3 MoveDirection { get; private set; }

        /// <summary>地面に立っているか</summary>
        public bool IsGrounded => controller != null && controller.isGrounded;

        CharacterController controller;
        DeskStageControls controls;
        readonly JumpTimer jumpTimer = new JumpTimer();
        float verticalSpeed;
        // 空中で使ったジャンプの回数（接地すると 0 に戻る）
        int airJumpsUsed;
        // この時刻までは入力を受け付けない（落ちて戻った直後など）
        float inputLockedUntil = float.NegativeInfinity;

        /// <summary>しばらく入力を受け付けない（Step 15：落ちて戻った直後の 0.5 秒）。重力は働く。</summary>
        public void LockInput(float seconds)
        {
            inputLockedUntil = Time.time + seconds;
        }

        // テスト用の入力（Step 8 の PlayMode テスト）。使っている間は DeskStageControls を読まない
        bool useTestInput;
        Vector2 testMove;
        bool testJump;

        /// <summary>テストから、スティックの値を入れる。</summary>
        public void SetTestInput(Vector2 move)
        {
            useTestInput = true;
            testMove = move;
        }

        /// <summary>テストから、ジャンプを1回押す。</summary>
        public void PressJumpForTest()
        {
            useTestInput = true;
            testJump = true;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            settings.ApplyTo(controller);
            controls = new DeskStageControls();
        }

        void OnEnable() => controls.Player.Enable();

        void OnDisable() => controls.Player.Disable();

        void OnDestroy() => controls.Dispose();

        /// <summary>
        /// 瞬間移動（Step 5）。スタートやチェックポイントへ戻すときは必ずこれを使う。
        /// Character Controller を有効のまま位置を書き換えると、元の場所に戻されてしまう。
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            verticalSpeed = 0f;
            airJumpsUsed = 0;
            jumpTimer.Reset();
            Teleported?.Invoke();
        }

        void Update()
        {
            if (Paused)
            {
                return;
            }

            float dt = Time.deltaTime;
            float now = Time.time;

            // 1・2：入力と水平の速さ
            Vector2 stick = useTestInput ? testMove : controls.Player.Move.ReadValue<Vector2>();
            bool jumpPressed = useTestInput ? testJump : controls.Player.Jump.WasPressedThisFrame();
            testJump = false;
            if (now < inputLockedUntil)
            {
                stick = Vector2.zero;
                jumpPressed = false;
            }
            Stick = stick;
            Vector3 direction = DirectionProvider != null ? DirectionProvider(stick) : new Vector3(stick.x, 0f, stick.y);
            Vector3 horizontal = MotorMath.HorizontalVelocity(direction, settings.walkSpeed);
            MoveDirection = horizontal.sqrMagnitude > 1e-10f ? horizontal.normalized : Vector3.zero;

            // 3：接地中は小さな下向きの値に戻す
            bool grounded = controller.isGrounded;
            verticalSpeed = MotorMath.StickToGround(verticalSpeed, grounded);

            // 4：最後に接地した時刻と、最後にジャンプが押された時刻
            if (grounded)
            {
                jumpTimer.RecordGrounded(now);
                airJumpsUsed = 0;
            }
            if (jumpPressed)
            {
                jumpTimer.RecordJumpPressed(now);
            }

            // 5：ジャンプ
            if (jumpTimer.TryConsume(now, settings.jumpBufferTime, settings.coyoteTime))
            {
                verticalSpeed = settings.JumpVelocity;
            }
            else if (jumpPressed && !grounded && airJumpsUsed < settings.maxAirJumps)
            {
                // 空中でもう一度押したら、その場から、もう一度同じ高さまで跳ぶ（2 段ジャンプ）
                verticalSpeed = settings.JumpVelocity;
                airJumpsUsed++;
            }

            // 6：重力
            verticalSpeed = MotorMath.ApplyGravity(verticalSpeed, settings.gravity, settings.maxFallSpeed, dt);

            // 7：水平と上下を合わせて1回だけ動かす。
            // 段差を自動で登る機能（Step Offset）は地面にいるときだけ使う。空中でも働くと、
            // ジャンプの頂点（5cm）に 0.8cm が足されて、壁のはずの 5cm の箱に乗れてしまう（Step 8）
            controller.stepOffset = grounded ? settings.stepOffset : 0f;
            controller.Move((horizontal + ExternalVelocity + Vector3.up * verticalSpeed) * dt);

            // 8：動いているときは進む方向へなめらかに向く
            if (horizontal.sqrMagnitude > 1e-8f)
            {
                var target = Quaternion.LookRotation(horizontal, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, settings.turnSpeed * dt);
            }
        }
    }
}
