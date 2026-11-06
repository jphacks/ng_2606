using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>RampMesh：坂（くさび形）の形と、斜めの面の高さ。</summary>
    public class RampMeshTests
    {
        [Test]
        public void くさび形は_1辺が1の立方体の中に収まる()
        {
            var bounds = RampMesh.Wedge().bounds;

            Assert.That(bounds.center.magnitude, Is.LessThan(1e-4f));
            Assert.That(bounds.size.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(bounds.size.y, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(bounds.size.z, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void 低い端は底に付き_高い端は上まである()
        {
            foreach (var v in RampMesh.Wedge().vertices)
            {
                if (v.x < 0f) Assert.That(v.y, Is.EqualTo(-0.5f).Within(1e-4f), "低い端（-x）は、底の高さ");
            }
            Assert.That(System.Array.Exists(RampMesh.Wedge().vertices, v => v.x > 0f && v.y > 0.49f), Is.True, "高い端（+x）は、上まである");
        }

        [Test]
        public void 三角形の向きは_持っている法線と同じ向き()
        {
            var mesh = RampMesh.Wedge();
            var v = mesh.vertices;
            var n = mesh.normals;
            var t = mesh.triangles;
            for (var i = 0; i < t.Length; i += 3)
            {
                var geometric = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                Assert.That(Vector3.Dot(geometric.normalized, n[t[i]]), Is.GreaterThan(0.99f), $"三角形 {i / 3} の表が、外を向いていない");
            }
        }

        [Test]
        public void 斜めの面は_箱の座標で_y_が_x_に等しい()
        {
            foreach (var p in RampMesh.Surface().vertices)
            {
                Assert.That(p.y, Is.EqualTo(RampMesh.SurfaceLocalY(p.x)).Within(1e-4f));
            }
        }

        [Test]
        public void 浮かせた斜めの面は_その分だけ上にある()
        {
            foreach (var p in RampMesh.SurfaceLifted(0.1f).vertices)
            {
                Assert.That(p.y, Is.EqualTo(p.x + 0.1f).Within(1e-4f));
            }
        }
    }
}
