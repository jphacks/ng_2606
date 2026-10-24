using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// カメラ基準の向き（C_gameplay.md の Step 7）を確かめる。
    /// </summary>
    public class CameraRelativeInputTests
    {
        const float Eps = 1e-5f;

        static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Eps);
            Assert.AreEqual(expected.y, actual.y, Eps);
            Assert.AreEqual(expected.z, actual.z, Eps);
        }

        [Test]
        public void 上に倒すと画面の奥へ進む()
        {
            // 見下ろしながら X の正の向きを見ているカメラ
            var rotation = Quaternion.Euler(40f, 90f, 0f);
            var dir = CameraRelativeInput.Direction(rotation * Vector3.forward, rotation * Vector3.up, Vector2.up);
            AssertVector(Vector3.right, dir);
        }

        [Test]
        public void 右に倒すと画面の右へ進む()
        {
            var rotation = Quaternion.Euler(40f, 0f, 0f);
            var dir = CameraRelativeInput.Direction(rotation * Vector3.forward, rotation * Vector3.up, Vector2.right);
            AssertVector(Vector3.right, dir);
        }

        [Test]
        public void 上下の成分は入らない()
        {
            var rotation = Quaternion.Euler(70f, 30f, 0f);
            var dir = CameraRelativeInput.Direction(rotation * Vector3.forward, rotation * Vector3.up, new Vector2(0.5f, 1f));
            Assert.AreEqual(0f, dir.y, Eps);
        }

        [Test]
        public void 真上から見ても画面の上へ進む()
        {
            // 真下を向くカメラ。画面の上は Z の正
            var rotation = Quaternion.Euler(90f, 0f, 0f);
            var dir = CameraRelativeInput.Direction(rotation * Vector3.forward, rotation * Vector3.up, Vector2.up);
            AssertVector(Vector3.forward, dir);
        }

        [Test]
        public void 真上の近くでも向きが急に変わらない()
        {
            var nearTop = Quaternion.Euler(89.5f, 0f, 0f);
            var top = Quaternion.Euler(90f, 0f, 0f);
            var a = CameraRelativeInput.Direction(nearTop * Vector3.forward, nearTop * Vector3.up, Vector2.up);
            var b = CameraRelativeInput.Direction(top * Vector3.forward, top * Vector3.up, Vector2.up);
            AssertVector(a, b);
        }
    }
}
