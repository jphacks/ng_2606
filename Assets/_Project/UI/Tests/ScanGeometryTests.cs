using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.UI.Tests
{
    public class ScanGeometryTests
    {
        static readonly Vector2[] SquareCcw = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

        [Test]
        public void 四角は中心と四隅で四つの三角形になる()
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            ScanGeometry.FanMesh(SquareCcw, 0.05f, v, t);
            Assert.AreEqual(5, v.Count);
            Assert.AreEqual(12, t.Count);
            Assert.AreEqual(new Vector3(0.5f, 0.05f, 0.5f), v[0]);
        }

        [Test]
        public void 三角形は上から見て時計回り()
        {
            // Unity は時計回りが表。真上から見える向きになっているか（法線が上）
            foreach (var outline in new[] { SquareCcw, new[] { SquareCcw[3], SquareCcw[2], SquareCcw[1], SquareCcw[0] } })
            {
                var v = new List<Vector3>();
                var t = new List<int>();
                ScanGeometry.FanMesh(outline, 0f, v, t);
                for (int i = 0; i < t.Count; i += 3)
                {
                    var n = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                    Assert.Greater(n.y, 0f);
                }
            }
        }

        [Test]
        public void 点が足りなければ空()
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            ScanGeometry.FanMesh(new[] { Vector2.zero, Vector2.one }, 0f, v, t);
            Assert.AreEqual(0, v.Count);
        }

        [Test]
        public void ふちを半分なぞると長方形の半周まで()
        {
            // 幅2・奥行き1：周は6。半分の3で、左辺（1）と奥の辺（2）を通り、奥の右の角まで
            var points = ScanGeometry.TraceRectangle(new Vector2(2f, 1f), 0.5f, 0f);
            Assert.AreEqual(3, points.Count);
            Assert.AreEqual(new Vector3(1f, 0f, 0.5f), points[2]);
        }

        [Test]
        public void ぜんぶなぞると元の角に戻る()
        {
            var points = ScanGeometry.TraceRectangle(new Vector2(2f, 1f), 1f, 0f);
            Assert.AreEqual(5, points.Count);
            Assert.AreEqual(points[0], points[4]);
        }

        [Test]
        public void ぽんの動きは0から始まり1で終わる()
        {
            Assert.AreEqual(0f, ScanGeometry.PopCurve(0f), 1e-5f);
            Assert.AreEqual(1f, ScanGeometry.PopCurve(1f), 1e-5f);
            Assert.Greater(ScanGeometry.PopCurve(0.7f), 1f);
        }
    }
}
