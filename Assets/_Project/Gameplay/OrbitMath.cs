using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 視点のカメラ（C_gameplay.md の Step 6）の計算部分。MonoBehaviour から切り離して EditMode でテストする。
    /// </summary>
    public static class OrbitMath
    {
        /// <summary>
        /// 見る点のまわりの、カメラの位置。yaw は水平に回る角度、pitch は見下ろす角度（度）。
        /// yaw 0 のときはキャラの後ろ（Z の負の側）から見る。
        /// </summary>
        public static Vector3 CameraPosition(Vector3 focus, float yawDegrees, float pitchDegrees, float distance)
        {
            var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            return focus - rotation * Vector3.forward * distance;
        }

        /// <summary>
        /// 長方形の机全体が画面に入るカメラの距離。机の対角線が縦の画角に収まるようにする。
        /// </summary>
        public static float OverviewDistance(float deskWidth, float deskDepth, float verticalFovDegrees, float margin = 1.1f)
        {
            float halfDiagonal = 0.5f * Mathf.Sqrt(deskWidth * deskWidth + deskDepth * deskDepth);
            float halfFov = Mathf.Max(verticalFovDegrees, 1f) * 0.5f * Mathf.Deg2Rad;
            return halfDiagonal / Mathf.Tan(halfFov) * margin;
        }

        /// <summary>
        /// 0〜1 の進み具合を、はじめとおわりがゆっくりな動きに直す。
        /// </summary>
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
