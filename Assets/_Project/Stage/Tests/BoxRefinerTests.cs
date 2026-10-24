using System.Collections.Generic;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>BoxRefiner と、それを使う BoxBuilderCore の仕上げ（B の Step 7）。</summary>
    public class BoxRefinerTests
    {
        const float Tolerance = 1e-5f;
        const float HeightMerge = 0.008f;
        const float GapClose = 0.016f;

        /// <summary>XZ の範囲と高さから、回していない箱を作る。</summary>
        static BoxData Box(string id, float xMin, float xMax, float zMin, float zMax, float height)
        {
            return new BoxData
            {
                id = id,
                center = new[] { (xMin + xMax) / 2f, height / 2f, (zMin + zMax) / 2f },
                size = new[] { xMax - xMin, height, zMax - zMin },
            };
        }

        static float XMin(BoxData b) => b.center[0] - b.size[0] / 2f;
        static float XMax(BoxData b) => b.center[0] + b.size[0] / 2f;

        static SurfaceData Surface(int id, float height, float xMin, float xMax, float zMin, float zMax)
        {
            return new SurfaceData
            {
                Id = id,
                Height = height,
                Outline = new[] { new Vector2(xMin, zMin), new Vector2(xMax, zMin), new Vector2(xMax, zMax), new Vector2(xMin, zMax) },
            };
        }

        [Test]
        public void 高さの差5mmの2つの箱がそろう()
        {
            var boxes = new[] { Box("box-01", -0.1f, 0f, -0.05f, 0.05f, 0.020f), Box("box-02", 0f, 0.1f, -0.05f, 0.05f, 0.025f) };

            var result = BoxRefiner.Refine(boxes, HeightMerge, GapClose);

            Assert.That(result[0].size[1], Is.EqualTo(0.025f).Within(Tolerance), "低い方が高い方にそろう");
            Assert.That(result[0].center[1], Is.EqualTo(0.0125f).Within(Tolerance), "中心の高さは高さの半分");
            Assert.That(result[1].size[1], Is.EqualTo(0.025f).Within(Tolerance));
            Assert.That(boxes[0].size[1], Is.EqualTo(0.020f).Within(Tolerance), "渡した箱は変えない");
        }

        [Test]
        public void 離れた箱は高さをそろえない()
        {
            var boxes = new[] { Box("box-01", -0.2f, -0.1f, -0.05f, 0.05f, 0.020f), Box("box-02", 0.1f, 0.2f, -0.05f, 0.05f, 0.025f) };

            var result = BoxRefiner.Refine(boxes, HeightMerge, GapClose);

            Assert.That(result[0].size[1], Is.EqualTo(0.020f).Within(Tolerance));
        }

        [Test]
        public void 高さの差が大きい箱はそろえない()
        {
            var boxes = new[] { Box("box-01", -0.1f, 0f, -0.05f, 0.05f, 0.02f), Box("box-02", 0f, 0.1f, -0.05f, 0.05f, 0.03f) };

            var result = BoxRefiner.Refine(boxes, HeightMerge, GapClose);

            Assert.That(result[0].size[1], Is.EqualTo(0.02f).Within(Tolerance));
            Assert.That(result[1].size[1], Is.EqualTo(0.03f).Within(Tolerance));
        }

        [Test]
        public void すき間1cmの2つの箱がくっつく()
        {
            // 左の低い箱と、1cm あけた右の高い箱
            var boxes = new[] { Box("box-01", -0.1f, 0f, -0.05f, 0.05f, 0.02f), Box("box-02", 0.01f, 0.11f, -0.05f, 0.05f, 0.14f) };

            var result = BoxRefiner.Refine(boxes, HeightMerge, GapClose);

            Assert.That(XMax(result[0]), Is.EqualTo(0.01f).Within(Tolerance), "低い方の箱が右へ広がる");
            Assert.That(XMin(result[0]), Is.EqualTo(-0.1f).Within(Tolerance), "反対側の辺は動かない");
            Assert.That(XMin(result[1]), Is.EqualTo(0.01f).Within(Tolerance), "高い方の箱はそのまま");
            Assert.That(result[1].size[0], Is.EqualTo(0.1f).Within(Tolerance));
        }

        [Test]
        public void 右の箱が低いときは右の箱が左へ広がる()
        {
            var boxes = new[] { Box("box-01", -0.1f, 0f, -0.05f, 0.05f, 0.14f), Box("box-02", 0.01f, 0.11f, -0.05f, 0.05f, 0.02f) };

            var result = BoxRefiner.Refine(boxes, HeightMerge, GapClose);

            Assert.That(XMax(result[0]), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(XMin(result[1]), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(XMax(result[1]), Is.EqualTo(0.11f).Within(Tolerance));
        }

        [Test]
        public void すき間2cmの箱はくっつけない()
        {
            var boxes = new[] { Box("box-01", -0.1f, 0f, -0.05f, 0.05f, 0.02f), Box("box-02", 0.02f, 0.12f, -0.05f, 0.05f, 0.14f) };

            var result = BoxRefiner.Refine(boxes, HeightMerge, GapClose);

            Assert.That(XMax(result[0]), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 積んだ物は上の物と下の物の箱が両方できる()
        {
            // 下：20×20cm で高さ 2cm。上：10×10cm を積んで高さ 5cm
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();

            core.Apply(new[] { Surface(1, 0.02f, -0.1f, 0.1f, -0.1f, 0.1f), Surface(2, 0.05f, -0.05f, 0.05f, -0.05f, 0.05f) }, boxes);

            Assert.That(boxes.Count, Is.EqualTo(2));
            Assert.That(boxes[0].size[1], Is.EqualTo(0.02f).Within(Tolerance));
            Assert.That(boxes[1].size[1], Is.EqualTo(0.05f).Within(Tolerance), "上の物の上面から机までの箱");
            Assert.That(boxes[1].center[1], Is.EqualTo(0.025f).Within(Tolerance));
        }

        [Test]
        public void 高さ5mm未満の上面は箱にしない()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();

            core.Apply(new[] { Surface(1, 0.004f, -0.1f, 0.1f, -0.1f, 0.1f) }, boxes);

            Assert.That(boxes, Is.Empty);
        }

        [Test]
        public void manualの箱は上面が変わっても変わらない()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();
            core.Apply(new[] { Surface(1, 0.02f, -0.1f, 0.1f, -0.1f, 0.1f) }, boxes);
            boxes[0].source = BoxData.SourceManual; // 補助で直した（B の Step 9 で行う）
            var before = boxes[0];

            var changed = core.Apply(new[] { Surface(1, 0.03f, -0.1f, 0.12f, -0.1f, 0.1f) }, boxes);
            var removed = core.Apply(new SurfaceData[0], boxes);

            Assert.That(changed, Is.Empty);
            Assert.That(removed, Is.Empty, "上面が消えても manual の箱は消さない");
            Assert.That(boxes.Count, Is.EqualTo(1));
            Assert.That(boxes[0], Is.SameAs(before));
            Assert.That(boxes[0].size[1], Is.EqualTo(0.02f).Within(Tolerance));
        }

        [Test]
        public void そろえた箱は次の上面の知らせで揺れない()
        {
            var core = new BoxBuilderCore();
            var boxes = new List<BoxData>();
            var surfaces = new[] { Surface(1, 0.020f, -0.1f, 0f, -0.05f, 0.05f), Surface(2, 0.025f, 0f, 0.1f, -0.05f, 0.05f) };
            core.Apply(surfaces, boxes);

            var changes = core.Apply(surfaces, boxes);

            Assert.That(changes, Is.Empty);
        }
    }
}
