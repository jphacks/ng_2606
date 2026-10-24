using System.Collections.Generic;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>BoxBuilderCore：上面1つにつき箱1つを作り・直し・消す。</summary>
    public class BoxBuilderCoreTests
    {
        const float Tolerance = 1e-5f;

        /// <summary>XZ の中心と幅・奥行きから、四角い上面を作る。</summary>
        static SurfaceData Square(int id, float height, float cx, float cz, float width, float depth)
        {
            var hw = width / 2f;
            var hd = depth / 2f;
            return new SurfaceData
            {
                Id = id,
                Height = height,
                Outline = new[]
                {
                    new Vector2(cx - hw, cz - hd),
                    new Vector2(cx + hw, cz - hd),
                    new Vector2(cx + hw, cz + hd),
                    new Vector2(cx - hw, cz + hd),
                },
            };
        }

        static void AssertVector(Vector3 expected, float[] actual)
        {
            Assert.That(Vector3.Distance(expected, Vec3.To(actual)), Is.LessThan(Tolerance),
                $"expected {expected} but was {Vec3.To(actual)}");
        }

        [Test]
        public void 四角い上面1つから箱が1つできる()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();

            var changes = core.Apply(new[] { Square(7, 0.02f, -0.22f, 0f, 0.15f, 0.22f) }, boxes);

            Assert.That(boxes.Count, Is.EqualTo(1));
            var box = boxes[0];
            Assert.That(box.id, Is.EqualTo("box-01"));
            Assert.That(box.source, Is.EqualTo(BoxData.SourceAuto));
            AssertVector(new Vector3(-0.22f, 0.01f, 0f), box.center);
            // 長い方の辺（前後の 22cm）が幅になり、90度回る
            AssertVector(new Vector3(0.22f, 0.02f, 0.15f), box.size);
            Assert.That(box.yawDegrees, Is.EqualTo(90f).Within(1e-3f));
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].change, Is.EqualTo(BoxChange.Added));
        }

        [Test]
        public void 回した上面から回した箱ができる()
        {
            // 15cm×22cm の上面を 30度回した形
            var rot = Quaternion.Euler(0f, 0f, 30f);
            var corners = new[] { new Vector2(-0.075f, -0.11f), new Vector2(0.075f, -0.11f), new Vector2(0.075f, 0.11f), new Vector2(-0.075f, 0.11f) };
            var outline = new Vector2[corners.Length];
            for (var i = 0; i < corners.Length; i++)
            {
                outline[i] = rot * corners[i];
            }
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();

            core.Apply(new[] { new SurfaceData { Id = 1, Height = 0.02f, Outline = outline } }, boxes);

            // RectFit は長い方の辺を幅にする
            Assert.That(boxes[0].size[0], Is.EqualTo(0.22f).Within(1e-3f));
            Assert.That(boxes[0].size[2], Is.EqualTo(0.15f).Within(1e-3f));
            Assert.That(boxes[0].yawDegrees, Is.Not.EqualTo(0f).Within(1f));
        }

        [Test]
        public void 上面が変わったら同じidの箱が変わる()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();
            core.Apply(new[] { Square(1, 0.02f, 0f, 0f, 0.10f, 0.10f) }, boxes);

            var changes = core.Apply(new[] { Square(1, 0.03f, 0.05f, 0f, 0.12f, 0.10f) }, boxes);

            Assert.That(boxes.Count, Is.EqualTo(1));
            Assert.That(boxes[0].id, Is.EqualTo("box-01"));
            AssertVector(new Vector3(0.05f, 0.015f, 0f), boxes[0].center);
            AssertVector(new Vector3(0.12f, 0.03f, 0.10f), boxes[0].size);
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].change, Is.EqualTo(BoxChange.Modified));
        }

        [Test]
        public void 上面が変わらなければ知らせない()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();
            var surfaces = new[] { Square(1, 0.02f, 0f, 0f, 0.10f, 0.10f) };
            core.Apply(surfaces, boxes);

            var changes = core.Apply(surfaces, boxes);

            Assert.That(changes, Is.Empty);
        }

        [Test]
        public void 上面が消えたら箱が消える()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();
            core.Apply(new[] { Square(1, 0.02f, -0.2f, 0f, 0.1f, 0.1f), Square(2, 0.14f, 0.2f, 0f, 0.2f, 0.2f) }, boxes);

            var changes = core.Apply(new[] { Square(2, 0.14f, 0.2f, 0f, 0.2f, 0.2f) }, boxes);

            Assert.That(boxes.Count, Is.EqualTo(1));
            Assert.That(boxes[0].id, Is.EqualTo("box-02"));
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0].boxId, Is.EqualTo("box-01"));
            Assert.That(changes[0].change, Is.EqualTo(BoxChange.Removed));
        }

        [Test]
        public void 補助で足した箱には触らない()
        {
            var core = new BoxBuilderCore();
            var manual = new BoxData { id = "box-01", source = BoxData.SourceManual, center = new[] { 0f, 0.025f, 0f }, size = new[] { 0.05f, 0.05f, 0.05f } };
            var boxes = new List<BoxData> { manual };

            core.Apply(new[] { Square(1, 0.02f, 0.2f, 0f, 0.1f, 0.1f) }, boxes);
            core.Apply(new SurfaceData[0], boxes);

            Assert.That(boxes.Count, Is.EqualTo(1));
            Assert.That(boxes[0], Is.SameAs(manual));
        }
    }
}
