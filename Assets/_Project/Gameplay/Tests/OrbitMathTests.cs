using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// 視点のカメラ（C_gameplay.md の Step 6）の計算を確かめる。
    /// </summary>
    public class OrbitMathTests
    {
        const float Eps = 1e-5f;

        [Test]
        public void 向き0ではキャラの後ろから見る()
        {
            var pos = OrbitMath.CameraPosition(Vector3.zero, 0f, 0f, 0.35f);
            Assert.AreEqual(0f, pos.x, Eps);
            Assert.AreEqual(0f, pos.y, Eps);
            Assert.AreEqual(-0.35f, pos.z, Eps);
        }

        [Test]
        public void 見下ろす角度の分だけ上にいる()
        {
            var pos = OrbitMath.CameraPosition(Vector3.zero, 0f, 40f, 0.35f);
            Assert.AreEqual(0.35f * Mathf.Sin(40f * Mathf.Deg2Rad), pos.y, Eps);
            Assert.AreEqual(0.35f, pos.magnitude, Eps);
        }

        [Test]
        public void 横に90度回ると左から見る()
        {
            var pos = OrbitMath.CameraPosition(Vector3.zero, 90f, 0f, 0.35f);
            Assert.AreEqual(-0.35f, pos.x, Eps);
            Assert.AreEqual(0f, pos.z, Eps);
        }

        [Test]
        public void 机全体が入る距離は机が大きいほど遠い()
        {
            float small = OrbitMath.OverviewDistance(0.6f, 0.4f, 60f);
            float large = OrbitMath.OverviewDistance(1.2f, 0.8f, 60f);
            Assert.Greater(large, small);
            Assert.AreEqual(small * 2f, large, Eps);
        }

        [Test]
        public void 机全体が入る距離では対角線が画角に収まる()
        {
            float d = OrbitMath.OverviewDistance(0.6f, 0.8f, 60f, 1f);
            // 対角線の半分 0.5m が、画角の半分 30 度にちょうど収まる
            Assert.AreEqual(0.5f / Mathf.Tan(30f * Mathf.Deg2Rad), d, Eps);
        }

        [Test]
        public void 寄りの動きははじめとおわりが0と1()
        {
            Assert.AreEqual(0f, OrbitMath.Ease(0f));
            Assert.AreEqual(1f, OrbitMath.Ease(1f));
            Assert.AreEqual(0.5f, OrbitMath.Ease(0.5f), Eps);
            Assert.AreEqual(1f, OrbitMath.Ease(2f));
        }
    }
}
