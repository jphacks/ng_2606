using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Shared.Tests
{
    /// <summary>RectFit（点を囲む最小面積の長方形）の確認。A の Step 7。</summary>
    public class RectFitTests
    {
        const float Tol = 1e-3f;

        /// <summary>幅 w、奥行き d の長方形の四隅を、中心 c、回転 yaw（度）で置く。</summary>
        static List<Vector2> Corners(Vector2 c, float w, float d, float yaw)
        {
            var q = Quaternion.Euler(0f, yaw, 0f);
            var list = new List<Vector2>();
            foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
            {
                var p = q * new Vector3(sx * w / 2f, 0f, sz * d / 2f);
                list.Add(c + new Vector2(p.x, p.z));
            }

            return list;
        }

        static void AssertRect(RectFitResult r, Vector2 c, float w, float d, float yaw)
        {
            Assert.AreEqual(c.x, r.center.x, Tol);
            Assert.AreEqual(c.y, r.center.y, Tol);
            Assert.AreEqual(w, r.width, Tol);
            Assert.AreEqual(d, r.depth, Tol);
            Assert.AreEqual(yaw, r.yawDegrees, Tol);
        }

        [Test]
        public void 軸に平行な長方形()
        {
            var r = RectFit.Fit(Corners(new Vector2(1f, 2f), 4f, 2f, 0f));
            AssertRect(r, new Vector2(1f, 2f), 4f, 2f, 0f);
        }

        [Test]
        public void 正方形は一辺の長さが幅と奥行きになる()
        {
            var r = RectFit.Fit(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f), new Vector2(0f, 2f),
            });
            Assert.AreEqual(1f, r.center.x, Tol);
            Assert.AreEqual(1f, r.center.y, Tol);
            Assert.AreEqual(2f, r.width, Tol);
            Assert.AreEqual(2f, r.depth, Tol);
        }

        [Test]
        public void 回した長方形()
        {
            var r = RectFit.Fit(Corners(new Vector2(-1f, 3f), 4f, 2f, 30f));
            AssertRect(r, new Vector2(-1f, 3f), 4f, 2f, 30f);
        }

        [Test]
        public void 奥行きの方向に回した長方形でも長い方が幅になる()
        {
            // 幅 4 を 120 度回す。長い辺の向きは 120 度のまま返る
            var r = RectFit.Fit(Corners(Vector2.zero, 4f, 2f, 120f));
            AssertRect(r, Vector2.zero, 4f, 2f, 120f);
        }

        [Test]
        public void 点が3つだけ()
        {
            // 直角三角形を囲む最小の面積は 12（3 x 4 でも、斜辺に沿った 5 x 2.4 でも同じ。どちらを選んでもよい）
            var r = RectFit.Fit(new List<Vector2> { new Vector2(0f, 0f), new Vector2(3f, 0f), new Vector2(0f, 4f) });
            Assert.AreEqual(12f, r.width * r.depth, Tol);
            Assert.GreaterOrEqual(r.width, r.depth);
        }

        [Test]
        public void 点が1つだけなら大きさ0()
        {
            var r = RectFit.Fit(new List<Vector2> { new Vector2(2f, 5f) });
            AssertRect(r, new Vector2(2f, 5f), 0f, 0f, 0f);
        }

        [Test]
        public void 全部同じ点なら大きさ0()
        {
            var r = RectFit.Fit(new List<Vector2> { new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f) });
            AssertRect(r, new Vector2(1f, 1f), 0f, 0f, 0f);
        }

        [Test]
        public void 点が無くてもエラーにならない()
        {
            Assert.DoesNotThrow(() => RectFit.Fit(new List<Vector2>()));
            Assert.DoesNotThrow(() => RectFit.Fit(null));
            Assert.AreEqual(0f, RectFit.Fit(new List<Vector2>()).width);
        }

        [Test]
        public void 点が2つなら線分に沿った幅と奥行き0()
        {
            var r = RectFit.Fit(new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 3f) });
            Assert.AreEqual(3f, r.width, Tol);
            Assert.AreEqual(0f, r.depth, Tol);
            Assert.AreEqual(0f, r.center.x, Tol);
            Assert.AreEqual(1.5f, r.center.y, Tol);
            // 線分は Z 方向。Euler(0, 90, 0) でローカル X が Z 方向（-Z）を向く
            Assert.AreEqual(90f, r.yawDegrees, Tol);
        }

        [Test]
        public void 一直線に並ぶ点は奥行き0()
        {
            var r = RectFit.Fit(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(2f, 2f), new Vector2(3f, 3f),
            });
            Assert.AreEqual(3f * Mathf.Sqrt(2f), r.width, Tol);
            Assert.AreEqual(0f, r.depth, Tol);
        }

        [Test]
        public void ばらついた点でも四隅の長方形が求まる()
        {
            var random = new System.Random(12345);
            var corners = Corners(new Vector2(0.5f, -0.5f), 3f, 1.5f, 25f);
            var points = new List<Vector2>(corners);
            var q = Quaternion.Euler(0f, 25f, 0f);
            for (var i = 0; i < 200; i++)
            {
                var lx = (float)(random.NextDouble() - 0.5) * 3f;
                var lz = (float)(random.NextDouble() - 0.5) * 1.5f;
                var p = q * new Vector3(lx, 0f, lz);
                points.Add(new Vector2(0.5f + p.x, -0.5f + p.z));
            }

            // 順番を崩す
            points.Reverse();
            var r = RectFit.Fit(points);
            AssertRect(r, new Vector2(0.5f, -0.5f), 3f, 1.5f, 25f);
        }

        [Test]
        public void 入力の並びは変わらない()
        {
            var points = new List<Vector2> { new Vector2(3f, 0f), new Vector2(0f, 0f), new Vector2(0f, 4f) };
            RectFit.Fit(points);
            Assert.AreEqual(new Vector2(3f, 0f), points[0]);
            Assert.AreEqual(new Vector2(0f, 0f), points[1]);
            Assert.AreEqual(new Vector2(0f, 4f), points[2]);
        }
    }
}
