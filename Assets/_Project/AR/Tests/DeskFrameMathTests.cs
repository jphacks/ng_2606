using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.AR.Tests
{
    /// <summary>DeskFrameMath（机の決定の計算）の確認。A の Step 8。</summary>
    public class DeskFrameMathTests
    {
        const float Tol = 1e-3f;

        /// <summary>幅 w、奥行き d の長方形の四隅（世界座標、高さ y）を、中心 c、回転 yaw（度）で置く。</summary>
        static List<Vector3> Rect(Vector3 c, float w, float d, float yaw)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            var list = new List<Vector3>();
            foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
            {
                list.Add(c + q * new Vector3(sx * w / 2f, 0f, sz * d / 2f));
            }

            return list;
        }

        [Test]
        public void 軸に平行な机は中心の高さに原点が来る()
        {
            var fit = DeskFrameMath.FitDesk(Rect(new Vector3(1f, 0.7f, 2f), 1.2f, 0.6f, 0f), 0.7f);
            Assert.AreEqual(1f, fit.Position.x, Tol);
            Assert.AreEqual(0.7f, fit.Position.y, Tol);
            Assert.AreEqual(2f, fit.Position.z, Tol);
            Assert.AreEqual(0f, fit.YawDegrees, Tol);
            Assert.AreEqual(1.2f, fit.Size.x, Tol);
            Assert.AreEqual(0.6f, fit.Size.y, Tol);
        }

        [Test]
        public void 回した机でも長い方の辺がXになる()
        {
            var fit = DeskFrameMath.FitDesk(Rect(new Vector3(0f, 0.7f, 0f), 1.2f, 0.6f, 40f), 0.7f);
            Assert.AreEqual(40f, fit.YawDegrees, Tol);
            Assert.AreEqual(1.2f, fit.Size.x, Tol);
            Assert.AreEqual(0.6f, fit.Size.y, Tol);
        }

        [Test]
        public void 奥行きの方向に長い机でも幅が長い方になる()
        {
            // Z 方向に長い机（X 0.6、Z 1.2）。長い方を X にするため 90 度回る
            var fit = DeskFrameMath.FitDesk(Rect(Vector3.zero, 0.6f, 1.2f, 0f), 0f);
            Assert.AreEqual(90f, fit.YawDegrees, Tol);
            Assert.AreEqual(1.2f, fit.Size.x, Tol);
            Assert.AreEqual(0.6f, fit.Size.y, Tol);
        }

        [Test]
        public void StageRootの向きでローカルXが長い辺に合う()
        {
            var fit = DeskFrameMath.FitDesk(Rect(new Vector3(3f, 0.7f, -1f), 1.2f, 0.6f, 25f), 0.7f);
            var rootRotation = Quaternion.Euler(0f, fit.YawDegrees, 0f);
            var corner = new Vector3(3f, 0.7f, -1f) + rootRotation * new Vector3(0.6f, 0f, 0.3f);
            // 長方形の四隅の 1 つが、ローカル (0.6, 0.3) に来る
            var found = false;
            foreach (var p in Rect(new Vector3(3f, 0.7f, -1f), 1.2f, 0.6f, 25f))
            {
                if ((p - corner).magnitude < Tol) found = true;
            }

            Assert.IsTrue(found);
        }

        [Test]
        public void 机が広がっても原点を中心にした大きさになる()
        {
            var root = new Pose(new Vector3(1f, 0.7f, 2f), Quaternion.Euler(0f, 30f, 0f));
            // 最初の机（1.2 x 0.6）と、+X 側に 0.4 m 広がった形
            var grown = new List<Vector3>(Rect(root.position, 1.2f, 0.6f, 30f));
            grown.Add(root.position + root.rotation * new Vector3(1.0f, 0f, 0f));

            var same = DeskFrameMath.SymmetricSize(Rect(root.position, 1.2f, 0.6f, 30f), root);
            Assert.AreEqual(1.2f, same.x, Tol);
            Assert.AreEqual(0.6f, same.y, Tol);

            var bigger = DeskFrameMath.SymmetricSize(grown, root);
            Assert.AreEqual(2.0f, bigger.x, Tol); // 原点から +X に 1.0 m → 左右対称で 2.0 m
            Assert.AreEqual(0.6f, bigger.y, Tol);
        }

        [Test]
        public void 点が多角形の中か外かを判定する()
        {
            var square = Rect(Vector3.zero, 2f, 2f, 0f);
            Assert.IsTrue(DeskFrameMath.ContainsXZ(square, new Vector3(0.5f, 5f, 0.5f)));
            Assert.IsFalse(DeskFrameMath.ContainsXZ(square, new Vector3(1.5f, 0f, 0f)));
            Assert.IsFalse(DeskFrameMath.ContainsXZ(square, new Vector3(0f, 0f, -1.5f)));
        }
    }
}
