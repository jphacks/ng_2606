using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>StageLayout：机の板と箱の置き方の計算。</summary>
    public class StageLayoutTests
    {
        const float Tolerance = 1e-5f;

        static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(Tolerance), $"expected {expected} but was {actual}");
        }

        [Test]
        public void 机の板は上面が高さ0になる()
        {
            var p = StageLayout.Desk(new DeskData { width = 0.9f, depth = 0.44f }, 0.01f);

            AssertVector(new Vector3(0f, -0.005f, 0f), p.position);
            AssertVector(new Vector3(0.9f, 0.01f, 0.44f), p.scale);
            Assert.That(p.position.y + p.scale.y / 2f, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 箱は中心と大きさがそのまま入る()
        {
            var box = new BoxData { center = new[] { -0.22f, 0.01f, 0f }, size = new[] { 0.15f, 0.02f, 0.22f } };

            var p = StageLayout.Box(box);

            AssertVector(new Vector3(-0.22f, 0.01f, 0f), p.position);
            AssertVector(new Vector3(0.15f, 0.02f, 0.22f), p.scale);
            Assert.That(p.position.y - p.scale.y / 2f, Is.EqualTo(0f).Within(Tolerance), "箱の底が机の天面にある");
        }

        [Test]
        public void 箱の回転は上から見た向きだけ()
        {
            var box = new BoxData { yawDegrees = 30f };

            var p = StageLayout.Box(box);

            Assert.That(p.rotation.eulerAngles.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(p.rotation.eulerAngles.y, Is.EqualTo(30f).Within(1e-3f));
            Assert.That(p.rotation.eulerAngles.z, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
