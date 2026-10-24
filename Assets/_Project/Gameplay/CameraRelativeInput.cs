using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// スティックの向きをカメラ基準に直す（C_gameplay.md の Step 7）。
    /// 視点をどこに回しても、スティックを上に倒すと画面の奥へ進むようにする。
    /// </summary>
    public class CameraRelativeInput : MonoBehaviour
    {
        /// <summary>この「前」の長さ未満なら、真下を向いているとみなす</summary>
        public const float MinForwardLength = 0.1f;

        public CharacterMotor motor;
        public Transform viewCamera;

        void OnEnable()
        {
            if (motor != null)
            {
                motor.DirectionProvider = ToWorld;
            }
        }

        void OnDisable()
        {
            if (motor != null)
            {
                motor.DirectionProvider = null;
            }
        }

        Vector3 ToWorld(Vector2 stick)
        {
            return Direction(viewCamera.forward, viewCamera.up, stick);
        }

        /// <summary>
        /// カメラの向きとスティックの値から、移動の向き（水平）を作る。
        /// </summary>
        public static Vector3 Direction(Vector3 cameraForward, Vector3 cameraUp, Vector2 stick)
        {
            // 1：カメラの前方向から上下の成分を取り除き、水平な「前」を作る
            var forward = new Vector3(cameraForward.x, 0f, cameraForward.z);

            // 2：真下を向いているときは、カメラの上方向を水平にしたものを「前」にする
            if (forward.magnitude < MinForwardLength)
            {
                forward = new Vector3(cameraUp.x, 0f, cameraUp.z);
            }
            forward.Normalize();

            // 3：「前」に直角な水平の向きを「右」にする
            var right = Vector3.Cross(Vector3.up, forward);
            return forward * stick.y + right * stick.x;
        }
    }
}
