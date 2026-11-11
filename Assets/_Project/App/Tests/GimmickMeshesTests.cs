using NUnit.Framework;
using UnityEngine;

namespace DeskStage.App.Tests
{
    /// <summary>炎と回復のエフェクトの、立体の形（GimmickMeshes）。</summary>
    public class GimmickMeshesTests
    {
        [Test]
        public void 炎は下が丸く上がとがる()
        {
            Assert.AreEqual(0f, GimmickMeshes.FlameRadius(0f), 1e-4f);
            Assert.AreEqual(0f, GimmickMeshes.FlameRadius(1f), 1e-4f);
            // いちばん太いのは、下の方
            Assert.Greater(GimmickMeshes.FlameRadius(0.3f), GimmickMeshes.FlameRadius(0.7f));
            Assert.AreEqual(0.5f, GimmickMeshes.FlameRadius(0.315f), 0.01f);
        }

        [Test]
        public void 炎の形は幅1高さ1で足もとが原点()
        {
            var mesh = GimmickMeshes.Flame();
            var b = mesh.bounds;
            Assert.AreEqual(0f, b.min.y, 1e-4f);
            Assert.AreEqual(1f, b.max.y, 1e-4f);
            Assert.AreEqual(1f, b.size.x, 0.02f);
            Assert.AreEqual(1f, b.size.z, 0.02f);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void 炎の絵は正面が明るく後ろが暗い()
        {
            var texture = GimmickMeshes.FlameTexture(false);
            var front = texture.GetPixelBilinear(0.02f, 0.4f);
            var back = texture.GetPixelBilinear(0.98f, 0.4f);
            Assert.Greater(front.grayscale, back.grayscale);
            // 上の先はうすい（消える）
            Assert.Less(texture.GetPixelBilinear(0.02f, 0.99f).a, 0.2f);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void 回復の十字は厚みがある()
        {
            var mesh = GimmickMeshes.Plus();
            var b = mesh.bounds;
            Assert.AreEqual(1f, b.size.x, 1e-4f);
            Assert.AreEqual(1f, b.size.y, 1e-4f);
            Assert.AreEqual(GimmickMeshes.Thickness, b.size.z, 1e-4f);
            Object.DestroyImmediate(mesh);
        }
    }
}
